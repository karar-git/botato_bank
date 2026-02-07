<script>
  import { api } from "./api.js";
  import { Bot, CircleX, SendHorizontal } from "lucide-svelte";

  let isOpen = false;
  let message = "";
  let messages = [];
  let loading = false;
  let messagesContainer;

  function toggle() {
    isOpen = !isOpen;
    if (isOpen && messages.length === 0) {
      messages = [
        {
          role: "assistant",
          content:
            "مرحبا! أنا مساعد Atomic Bank الذكي. كيف يمكنني مساعدتك اليوم؟",
        },
      ];
    }
  }

  function scrollToBottom() {
    if (messagesContainer) {
      setTimeout(() => {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
      }, 50);
    }
  }

  async function sendMessage() {
    const text = message.trim();
    if (!text || loading) return;

    messages = [...messages, { role: "user", content: text }];
    message = "";
    loading = true;
    scrollToBottom();

    // Build conversation history (exclude the greeting)
    const history = messages
      .slice(1)
      .map((m) => ({ role: m.role, content: m.content }));

    try {
      const res = await api.chat(text, history);
      messages = [...messages, { role: "assistant", content: res.reply }];
    } catch (err) {
      messages = [
        ...messages,
        {
          role: "assistant",
          content: "عذرا، حدث خطأ. حاول مرة أخرى لاحقا.",
        },
      ];
    } finally {
      loading = false;
      scrollToBottom();
    }
  }

  function handleKeydown(e) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  }
</script>

<svelte:head>
  <link rel="preconnect" href="https://fonts.googleapis.com" />
  <link
    rel="preconnect"
    href="https://fonts.gstatic.com"
    crossorigin="anonymous"
  />
  <link
    href="https://fonts.googleapis.com/css2?family=Cairo:wght@200..1000&family=Lexend:wght@100..900&display=swap"
    rel="stylesheet"
  />
</svelte:head>

<!-- Floating chat bubble -->
<div class="chat-widget">
  {#if isOpen}
    <div class="chat-panel">
      <div class="chat-header">
        <div class="chat-title">
          <span class="chat-icon"><Bot size={28} strokeWidth={2} /></span>
          <span class="title-text">المساعد البنكي</span>
        </div>
        <button class="close-btn" on:click={toggle}>
          <CircleX size={24} strokeWidth={2} />
        </button>
      </div>

      <div class="chat-messages" bind:this={messagesContainer}>
        {#each messages as msg}
          <div class="message {msg.role}">
            <div class="bubble">{msg.content}</div>
          </div>
        {/each}
        {#if loading}
          <div class="message assistant">
            <div class="bubble typing">
              <span class="dot"></span>
              <span class="dot"></span>
              <span class="dot"></span>
            </div>
          </div>
        {/if}
      </div>

      <div class="chat-input">
        <input
          type="text"
          bind:value={message}
          on:keydown={handleKeydown}
          placeholder="اكتب رسالتك..."
          disabled={loading}
        />
        <button
          class="send-btn"
          on:click={sendMessage}
          disabled={loading || !message.trim()}
        >
          <SendHorizontal size={20} strokeWidth={2} />
        </button>
      </div>
    </div>
  {/if}

  <button class="fab" on:click={toggle} class:open={isOpen}>
    <span class="fab-icon">
      {#if isOpen}
        <CircleX size={30} strokeWidth={2} />
      {:else}
        <Bot size={30} strokeWidth={2} />
      {/if}
    </span>
  </button>
</div>

<style>
  /* Fonts */
  .chat-widget {
    position: fixed;
    bottom: 24px;
    right: 24px;
    z-index: 9999;
    font-family: "Lexend", "Cairo", sans-serif;
  }

  /* FAB Button - Atomic Bank Style - Clean Modern Look */
  .fab {
    width: 64px;
    height: 64px;
    border-radius: 50%;
    border: none;
    background: linear-gradient(
      135deg,
      var(--brand-primary, #3498db) 0%,
      var(--brand-dark, #34495e) 100%
    );
    color: white;
    cursor: pointer;
    box-shadow: 0 8px 32px rgba(52, 152, 219, 0.35);
    transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0;
  }

  .fab:hover {
    transform: scale(1.08) translateY(-2px);
    box-shadow: 0 12px 40px rgba(52, 152, 219, 0.5);
  }

  .fab.open {
    background: linear-gradient(
      135deg,
      var(--accent, #f8c957) 0%,
      #d4a017 100%
    );
    box-shadow: 0 8px 32px rgba(248, 201, 87, 0.4);
  }

  .fab.open:hover {
    box-shadow: 0 12px 40px rgba(248, 201, 87, 0.55);
  }

  .fab-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 100%;
    line-height: 1;
  }

  .fab-icon {
    line-height: 1;
    position: relative;
    z-index: 1;
  }

  /* Chat Panel - Glassmorphism */
  .chat-panel {
    position: absolute;
    bottom: 80px;
    right: 0;
    width: 400px;
    max-height: 560px;
    background: rgba(255, 255, 255, 0.92);
    backdrop-filter: blur(25px);
    -webkit-backdrop-filter: blur(25px);
    border: 1px solid rgba(255, 255, 255, 0.6);
    border-radius: 24px;
    box-shadow: 0 12px 48px rgba(0, 0, 0, 0.12);
    display: flex;
    flex-direction: column;
    overflow: hidden;
    animation: slideUp 0.35s cubic-bezier(0.25, 0.8, 0.25, 1);
  }

  /* Animated gradient border for panel */
  .chat-panel::before {
    content: "";
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    border-radius: inherit;
    padding: 1.5px;
    background: linear-gradient(
      90deg,
      var(--brand-primary, #3498db),
      var(--accent, #f8c957),
      #27ae60,
      var(--brand-primary, #3498db)
    );
    background-size: 300% 300%;
    -webkit-mask:
      linear-gradient(#fff 0 0) content-box,
      linear-gradient(#fff 0 0);
    -webkit-mask-composite: xor;
    mask-composite: exclude;
    animation: borderRotate 4s linear infinite;
    pointer-events: none;
    z-index: 0;
  }

  @keyframes slideUp {
    from {
      opacity: 0;
      transform: translateY(24px) scale(0.95);
    }
    to {
      opacity: 1;
      transform: translateY(0) scale(1);
    }
  }

  /* Chat Header - Atomic Bank Dark Theme */
  .chat-header {
    background: linear-gradient(
      135deg,
      var(--brand-dark, #34495e) 0%,
      #2c3e50 100%
    );
    color: white;
    padding: 18px 22px;
    display: flex;
    align-items: center;
    justify-content: space-between;
    border-radius: 24px 24px 0 0;
    position: relative;
    z-index: 1;
  }

  .chat-title {
    display: flex;
    align-items: center;
    gap: 14px;
  }

  .title-text {
    font-family: "Cairo", "Lexend", sans-serif;
    font-weight: 600;
    font-size: 17px;
    direction: rtl;
  }

  .chat-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 36px;
    height: 36px;
    background: rgba(255, 255, 255, 0.15);
    border-radius: 50%;
    flex-shrink: 0;
  }

  .close-btn {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 36px;
    height: 36px;
    background: rgba(255, 255, 255, 0.1);
    border: none;
    color: white;
    cursor: pointer;
    padding: 0;
    border-radius: 50%;
    opacity: 0.85;
    transition: all 0.2s ease;
    flex-shrink: 0;
  }

  .close-btn:hover {
    opacity: 1;
    background: rgba(255, 255, 255, 0.2);
    transform: scale(1.05);
  }

  /* Chat Messages Area */
  .chat-messages {
    flex: 1;
    overflow-y: auto;
    padding: 20px;
    display: flex;
    flex-direction: column;
    gap: 14px;
    min-height: 320px;
    max-height: 380px;
    background: var(--bg-secondary, #ecf0f1);
    position: relative;
    z-index: 1;
  }

  .message {
    display: flex;
  }

  .message.user {
    justify-content: flex-end;
  }

  .message.assistant {
    justify-content: flex-start;
  }

  /* Message Bubbles */
  .bubble {
    max-width: 82%;
    padding: 12px 18px;
    border-radius: 18px;
    font-size: 14px;
    line-height: 1.6;
    word-wrap: break-word;
    direction: rtl;
    text-align: right;
    font-family: "Cairo", "Lexend", sans-serif;
  }

  .message.user .bubble {
    background: linear-gradient(
      135deg,
      var(--brand-primary, #3498db) 0%,
      var(--brand-dark, #34495e) 100%
    );
    color: white;
    border-bottom-right-radius: 6px;
    box-shadow: 0 4px 16px rgba(52, 152, 219, 0.25);
  }

  .message.assistant .bubble {
    background: rgba(255, 255, 255, 0.95);
    color: var(--text-main, #2c3e50);
    border: 1px solid rgba(226, 232, 240, 0.8);
    border-bottom-left-radius: 6px;
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.04);
  }

  /* Typing Indicator */
  .typing {
    display: flex;
    gap: 5px;
    padding: 14px 20px;
    align-items: center;
    justify-content: center;
  }

  .dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: var(--brand-primary, #3498db);
    animation: bounce 1.4s infinite ease-in-out;
  }

  .dot:nth-child(2) {
    animation-delay: 0.2s;
  }

  .dot:nth-child(3) {
    animation-delay: 0.4s;
  }

  @keyframes bounce {
    0%,
    80%,
    100% {
      transform: scale(0);
    }
    40% {
      transform: scale(1);
    }
  }

  /* Chat Input Area */
  .chat-input {
    display: flex;
    align-items: center;
    padding: 16px 18px;
    border-top: 1px solid rgba(226, 232, 240, 0.8);
    background: rgba(255, 255, 255, 0.98);
    gap: 10px;
    border-radius: 0 0 24px 24px;
    position: relative;
    z-index: 1;
  }

  .chat-input input {
    flex: 1;
    border: 1px solid #e2e8f0;
    border-radius: 24px;
    padding: 12px 18px;
    font-size: 14px;
    font-family: "Cairo", "Lexend", sans-serif;
    outline: none;
    transition: all 0.3s ease;
    direction: rtl;
    background: white;
    color: var(--text-main, #2c3e50);
  }

  .chat-input input::placeholder {
    color: var(--text-muted, #7f8c8d);
  }

  .chat-input input:focus {
    border-color: var(--brand-primary, #3498db);
    box-shadow: 0 0 0 3px rgba(52, 152, 219, 0.15);
  }

  .chat-input input:disabled {
    background: #f8fafc;
  }

  /* Send Button */
  .send-btn {
    width: 44px;
    height: 44px;
    border-radius: 50%;
    border: none;
    background: linear-gradient(
      135deg,
      var(--brand-primary, #3498db) 0%,
      var(--brand-dark, #34495e) 100%
    );
    color: white;
    font-size: 18px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.3s ease;
    box-shadow: 0 4px 16px rgba(52, 152, 219, 0.25);
  }

  .send-btn:not(:disabled):hover {
    transform: scale(1.08);
    box-shadow: 0 6px 20px rgba(52, 152, 219, 0.4);
  }

  .send-btn:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  /* Responsive */
  @media (max-width: 480px) {
    .chat-panel {
      width: calc(100vw - 32px);
      right: -8px;
      bottom: 76px;
    }

    .chat-widget {
      bottom: 16px;
      right: 16px;
    }

    .fab {
      width: 56px;
      height: 56px;
      font-size: 24px;
    }
  }
</style>
