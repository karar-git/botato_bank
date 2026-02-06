from flask import Flask, request, jsonify
from ai import validate_national_id, chatbot

app = Flask(__name__)


@app.route("/validate-id", methods=["POST"])
def validate_id():
    if "image" not in request.files:
        return jsonify(
            {"valid": False, "idNumber": None, "error": "No image file provided"}
        ), 400

    file = request.files["image"]
    if file.filename == "":
        return jsonify(
            {"valid": False, "idNumber": None, "error": "Empty filename"}
        ), 400

    allowed = {"image/jpeg", "image/png", "image/jpg", "image/webp"}
    if file.content_type not in allowed:
        return jsonify(
            {"valid": False, "idNumber": None, "error": "Invalid file type"}
        ), 400

    image_bytes = file.read()
    if len(image_bytes) == 0:
        return jsonify({"valid": False, "idNumber": None, "error": "Empty file"}), 400

    result = validate_national_id(image_bytes)
    status = 200 if result["valid"] else 400
    return jsonify(result), status


@app.route("/chat", methods=["POST"])
def chat():
    """
    Chat endpoint. Expects JSON:
    {
        "message": "user's message",
        "conversationHistory": [{"role": "user"|"assistant", "content": "..."}],
        "userContext": {
            "userName": "...",
            "accounts": [...],
            "recentTransactions": [...]
        }
    }
    Returns: {"reply": "assistant's response"}
    """
    data = request.get_json()
    if not data or "message" not in data:
        return jsonify({"error": "Missing 'message' field"}), 400

    message = data["message"]
    history = data.get("conversationHistory", [])
    user_context = data.get("userContext", {})

    try:
        reply = chatbot.chat(message, history, user_context)
        return jsonify({"reply": reply}), 200
    except Exception as e:
        return jsonify({"error": f"Chat service error: {str(e)}"}), 500


@app.route("/health", methods=["GET"])
def health():
    return jsonify({"status": "healthy"}), 200


if __name__ == "__main__":
    import os

    port = int(os.environ.get("PORT", 8000))
    app.run(host="0.0.0.0", port=port)
