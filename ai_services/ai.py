import base64
import json
import os
from openai import OpenAI

# fal.ai OpenRouter endpoint — OpenAI-compatible
# Uses FAL_KEY for authentication, routes through OpenRouter to Gemini Flash 2.5
FAL_KEY = os.environ.get("FAL_KEY", "")

client = OpenAI(
    api_key=FAL_KEY,
    base_url="https://fal.run/openrouter/router/openai/v1",
    default_headers={"Authorization": f"Key {FAL_KEY}"},
)

VISION_MODEL = "google/gemini-2.5-flash"
CHAT_MODEL = "google/gemini-2.5-flash"


def validate_national_id(image_bytes: bytes) -> dict:
    """
    Takes an image of an ID card, sends it to Gemini Flash 2.5 via fal.ai,
    and checks if it's a valid البطاقة الوطنية (Moroccan national ID card).
    Returns {"valid": bool, "idNumber": str|None, "error": str|None}
    """
    base64_image = base64.b64encode(image_bytes).decode("utf-8")

    response = client.chat.completions.create(
        model=VISION_MODEL,
        messages=[
            {
                "role": "system",
                "content": (
                    "You are an ID card verification system. You analyze images of identification documents. "
                    "You must determine if the image is a valid Moroccan البطاقة الوطنية (Carte Nationale d'Identité). "
                    "If it is, extract the CIN number (usually 1-2 letters followed by digits, e.g. AB123456). "
                    "Respond ONLY with valid JSON, no markdown, no explanation."
                ),
            },
            {
                "role": "user",
                "content": [
                    {
                        "type": "text",
                        "text": (
                            "Analyze this image. Is it a Moroccan البطاقة الوطنية (national ID card)? "
                            "Look for indicators like: 'البطاقة الوطنية', 'CARTE NATIONALE', 'ROYAUME DU MAROC', 'المملكة المغربية'. "
                            "If valid, extract the CIN number. "
                            'Respond with JSON: {"valid": true/false, "idNumber": "XX123456" or null, "error": null or "reason"}'
                        ),
                    },
                    {
                        "type": "image_url",
                        "image_url": {
                            "url": f"data:image/jpeg;base64,{base64_image}",
                            "detail": "high",
                        },
                    },
                ],
            },
        ],
        max_tokens=200,
        temperature=0,
    )

    raw = response.choices[0].message.content.strip()

    # Strip markdown code fences if present
    if raw.startswith("```"):
        raw = raw.split("\n", 1)[-1].rsplit("```", 1)[0].strip()

    try:
        result = json.loads(raw)
        return {
            "valid": bool(result.get("valid", False)),
            "idNumber": result.get("idNumber"),
            "error": result.get("error"),
        }
    except json.JSONDecodeError:
        return {
            "valid": False,
            "idNumber": None,
            "error": "Failed to parse AI response",
        }


class BankChatbot:
    """
    A banking assistant chatbot that has context about the user's accounts
    and transaction history. The .NET backend sends the user's data along
    with each chat request so the chatbot can give informed answers.
    Uses Gemini Flash 2.5 via fal.ai OpenRouter.
    """

    SYSTEM_PROMPT = (
        "You are Atomic Bank's AI banking assistant. You help customers with their banking questions. "
        "You have access to the customer's account information and transaction history provided below. "
        "Use this data to answer questions about their balance, recent transactions, spending patterns, etc. "
        "Be concise, helpful, and professional. If asked about something outside banking, politely redirect. "
        "Always respond in the same language the user writes in (Arabic, French, or English). "
        "Never reveal sensitive details like full account numbers — use masked versions (e.g. ***1234). "
        "Amounts are in USD unless stated otherwise."
    )

    def __init__(self):
        self.client = OpenAI(
            api_key=FAL_KEY,
            base_url="https://fal.run/openrouter/router/openai/v1",
            default_headers={"Authorization": f"Key {FAL_KEY}"},
        )

    def chat(
        self, user_message: str, conversation_history: list, user_context: dict
    ) -> str:
        """
        Process a chat message with the user's banking context.

        Args:
            user_message: The user's latest message
            conversation_history: List of prior messages [{"role": "user"|"assistant", "content": "..."}]
            user_context: Dict with user's banking data from the .NET backend:
                {
                    "userName": "...",
                    "accounts": [{"accountNumber": "...", "type": "...", "balance": ..., "currency": "..."}],
                    "recentTransactions": [{"date": "...", "type": "...", "amount": ..., "description": "...", "balanceAfter": ...}]
                }
        Returns:
            The assistant's response text
        """
        # Build context summary from user data
        context_block = self._build_context(user_context)

        messages = [
            {"role": "system", "content": self.SYSTEM_PROMPT},
            {"role": "system", "content": f"Customer data:\n{context_block}"},
        ]

        # Add conversation history
        for msg in conversation_history:
            messages.append({"role": msg["role"], "content": msg["content"]})

        # Add the new user message
        messages.append({"role": "user", "content": user_message})

        response = self.client.chat.completions.create(
            model=CHAT_MODEL,
            messages=messages,
            max_tokens=500,
            temperature=0.7,
        )

        return response.choices[0].message.content.strip()

    def _build_context(self, user_context: dict) -> str:
        """Format user's banking data into a readable context string."""
        parts = []

        name = user_context.get("userName", "Customer")
        parts.append(f"Customer: {name}")

        accounts = user_context.get("accounts", [])
        if accounts:
            parts.append("\nAccounts:")
            for acc in accounts:
                masked = "***" + acc.get("accountNumber", "")[-4:]
                parts.append(
                    f"  - {acc.get('type', 'Account')} ({masked}): "
                    f"{acc.get('balance', 0):.2f} {acc.get('currency', 'USD')} "
                    f"[Status: {acc.get('status', 'Active')}]"
                )

        transactions = user_context.get("recentTransactions", [])
        if transactions:
            parts.append(f"\nRecent Transactions (last {len(transactions)}):")
            for tx in transactions:
                parts.append(
                    f"  - [{tx.get('date', '')}] {tx.get('type', '')}: "
                    f"{tx.get('amount', 0):+.2f} USD — {tx.get('description', 'N/A')} "
                    f"(Balance after: {tx.get('balanceAfter', 0):.2f})"
                )

        return "\n".join(parts)


# Singleton instance
chatbot = BankChatbot()
