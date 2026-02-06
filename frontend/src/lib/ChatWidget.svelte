<script>
  import { api } from './api.js';

  let isOpen = false;
  let message = '';
  let messages = [];
  let loading = false;
  let messagesContainer;

  function toggle() {
    isOpen = !isOpen;
    if (isOpen && messages.length === 0) {
      messages = [
        {
          role: 'assistant',
          content: 'مرحبا! أنا المساعد البنكي الذكي. كيف يمكنني مساعدتك اليوم؟',
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

    messages = [...messages, { role: 'user', content: text }];
    message = '';
    loading = true;
    scrollToBottom();

    // Build conversation history (exclude the greeting)
    const history = messages
      .slice(1)
      .map((m) => ({ role: m.role, content: m.content }));

    try {
      const res = await api.chat(text, history);
      messages = [...messages, { role: 'assistant', content: res.reply }];
    } catch (err) {
      messages = [
        ...messages,
        {
          role: 'assistant',
          content: 'عذرا، حدث خطأ. حاول مرة أخرى لاحقا.',
        },
      ];
    } finally {
      loading = false;
      scrollToBottom();
    }
  }

  function handleKeydown(e) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  }
</script>

<!-- Floating chat bubble -->
<div class="chat-widget">
  {#if isOpen}
    <div class="chat-panel">
      <div class="chat-header">
        <div class="chat-title">
          <span class="chat-icon">🤖</span>
          <span>المساعد البنكي</span>
        </div>
        <button class="close-btn" on:click={toggle}>&times;</button>
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
        <button class="send-btn" on:click={sendMessage} disabled={loading || !message.trim()}>
          &#10148;
        </button>
      </div>
    </div>
  {/if}

  <button class="fab" on:click={toggle} class:open={isOpen}>
    {#if isOpen}
      <span class="fab-icon">&times;</span>
    {:else}
      <span class="fab-icon">💬</span>
    {/if}
  </button>
</div>

<style>
  .chat-widget {
    position: fixed;
    bottom: 24px;
    right: 24px;
    z-index: 9999;
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  }

  .fab {
    width: 60px;
    height: 60px;
    border-radius: 50%;
    border: none;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    font-size: 28px;
    cursor: pointer;
    box-shadow: 0 4px 20px rgba(102, 126, 234, 0.4);
    transition: all 0.3s ease;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .fab:hover {
    transform: scale(1.1);
    box-shadow: 0 6px 28px rgba(102, 126, 234, 0.6);
  }

  .fab.open {
    background: linear-gradient(135deg, #e74c3c 0%, #c0392b 100%);
    box-shadow: 0 4px 20px rgba(231, 76, 60, 0.4);
  }

  .fab-icon {
    line-height: 1;
  }

  .chat-panel {
    position: absolute;
    bottom: 72px;
    right: 0;
    width: 380px;
    max-height: 520px;
    background: #fff;
    border-radius: 16px;
    box-shadow: 0 8px 40px rgba(0, 0, 0, 0.15);
    display: flex;
    flex-direction: column;
    overflow: hidden;
    animation: slideUp 0.3s ease;
  }

  @keyframes slideUp {
    from {
      opacity: 0;
      transform: translateY(20px);
    }
    to {
      opacity: 1;
      transform: translateY(0);
    }
  }

  .chat-header {
    background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
    color: white;
    padding: 16px 20px;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  .chat-title {
    display: flex;
    align-items: center;
    gap: 10px;
    font-weight: 600;
    font-size: 16px;
  }

  .chat-icon {
    font-size: 22px;
  }

  .close-btn {
    background: none;
    border: none;
    color: white;
    font-size: 24px;
    cursor: pointer;
    padding: 0;
    line-height: 1;
    opacity: 0.7;
    transition: opacity 0.2s;
  }

  .close-btn:hover {
    opacity: 1;
  }

  .chat-messages {
    flex: 1;
    overflow-y: auto;
    padding: 16px;
    display: flex;
    flex-direction: column;
    gap: 12px;
    min-height: 300px;
    max-height: 360px;
    background: #f8f9fa;
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

  .bubble {
    max-width: 80%;
    padding: 10px 14px;
    border-radius: 16px;
    font-size: 14px;
    line-height: 1.5;
    word-wrap: break-word;
    direction: rtl;
    text-align: right;
  }

  .message.user .bubble {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border-bottom-right-radius: 4px;
  }

  .message.assistant .bubble {
    background: white;
    color: #333;
    border: 1px solid #e0e0e0;
    border-bottom-left-radius: 4px;
  }

  .typing {
    display: flex;
    gap: 4px;
    padding: 12px 18px;
    align-items: center;
    justify-content: center;
  }

  .dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: #999;
    animation: bounce 1.4s infinite ease-in-out;
  }

  .dot:nth-child(2) {
    animation-delay: 0.2s;
  }

  .dot:nth-child(3) {
    animation-delay: 0.4s;
  }

  @keyframes bounce {
    0%, 80%, 100% {
      transform: scale(0);
    }
    40% {
      transform: scale(1);
    }
  }

  .chat-input {
    display: flex;
    align-items: center;
    padding: 12px 16px;
    border-top: 1px solid #e0e0e0;
    background: white;
    gap: 8px;
  }

  .chat-input input {
    flex: 1;
    border: 1px solid #ddd;
    border-radius: 24px;
    padding: 10px 16px;
    font-size: 14px;
    outline: none;
    transition: border-color 0.2s;
    direction: rtl;
  }

  .chat-input input:focus {
    border-color: #667eea;
  }

  .chat-input input:disabled {
    background: #f5f5f5;
  }

  .send-btn {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    border: none;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    font-size: 18px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: opacity 0.2s;
  }

  .send-btn:disabled {
    opacity: 0.4;
    cursor: not-allowed;
  }

  .send-btn:not(:disabled):hover {
    opacity: 0.9;
  }

  @media (max-width: 480px) {
    .chat-panel {
      width: calc(100vw - 32px);
      right: -8px;
      bottom: 68px;
    }

    .chat-widget {
      bottom: 16px;
      right: 16px;
    }
  }
</style>
