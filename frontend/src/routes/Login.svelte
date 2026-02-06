<script>
  import { createEventDispatcher } from 'svelte';
  import { api, setToken } from '../lib/api.js';

  const dispatch = createEventDispatcher();

  let mode = 'login'; // 'login' or 'register'
  let loading = false;
  let error = '';

  let email = '';
  let password = '';
  let fullName = '';

  async function handleSubmit() {
    loading = true;
    error = '';
    try {
      let result;
      if (mode === 'register') {
        result = await api.register({ fullName, email, password });
      } else {
        result = await api.login({ email, password });
      }
      dispatch('login', { token: result.token, user: result.user });
    } catch (err) {
      error = err.message || 'Authentication failed';
    } finally {
      loading = false;
    }
  }
</script>

<div class="auth-container">
  <div class="auth-card">
    <h2>{mode === 'login' ? 'Sign In' : 'Create Account'}</h2>
    <p class="auth-subtitle">
      {mode === 'login'
        ? 'Access your banking dashboard'
        : 'Start banking in seconds'}
    </p>

    {#if error}
      <div class="error-msg">{error}</div>
    {/if}

    <form on:submit|preventDefault={handleSubmit}>
      {#if mode === 'register'}
        <div class="field">
          <label for="fullName">Full Name</label>
          <input id="fullName" type="text" bind:value={fullName} required minlength="2" maxlength="100" placeholder="John Doe" />
        </div>
      {/if}

      <div class="field">
        <label for="email">Email</label>
        <input id="email" type="email" bind:value={email} required placeholder="you@example.com" />
      </div>

      <div class="field">
        <label for="password">Password</label>
        <input id="password" type="password" bind:value={password} required minlength="8" placeholder="Min 8 characters" />
      </div>

      <button type="submit" class="btn-primary" disabled={loading}>
        {#if loading}Processing...{:else}{mode === 'login' ? 'Sign In' : 'Register'}{/if}
      </button>
    </form>

    <p class="toggle-mode">
      {mode === 'login' ? "Don't have an account?" : 'Already have an account?'}
      <button class="link-btn" on:click={() => { mode = mode === 'login' ? 'register' : 'login'; error = ''; }}>
        {mode === 'login' ? 'Register' : 'Sign In'}
      </button>
    </p>
  </div>
</div>

<style>
  .auth-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 60vh;
  }
  .auth-card {
    background: white;
    border-radius: 12px;
    padding: 2.5rem;
    width: 100%;
    max-width: 420px;
    box-shadow: 0 4px 20px rgba(0,0,0,0.08);
  }
  h2 { font-size: 1.5rem; margin-bottom: 0.3rem; color: #1a1a2e; }
  .auth-subtitle { color: #718096; font-size: 0.9rem; margin-bottom: 1.5rem; }
  .field { margin-bottom: 1rem; }
  label { display: block; font-size: 0.85rem; font-weight: 600; margin-bottom: 0.3rem; color: #4a5568; }
  input {
    width: 100%;
    padding: 0.7rem 1rem;
    border: 1px solid #e2e8f0;
    border-radius: 6px;
    font-size: 0.95rem;
    transition: border-color 0.2s;
  }
  input:focus { outline: none; border-color: #4299e1; box-shadow: 0 0 0 3px rgba(66,153,225,0.15); }
  .btn-primary {
    width: 100%;
    padding: 0.75rem;
    background: linear-gradient(135deg, #1a1a2e, #16213e);
    color: white;
    border: none;
    border-radius: 6px;
    font-size: 1rem;
    font-weight: 600;
    cursor: pointer;
    margin-top: 0.5rem;
    transition: opacity 0.2s;
  }
  .btn-primary:hover { opacity: 0.9; }
  .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
  .error-msg {
    background: #fed7d7;
    color: #c53030;
    padding: 0.7rem 1rem;
    border-radius: 6px;
    font-size: 0.85rem;
    margin-bottom: 1rem;
  }
  .toggle-mode { text-align: center; margin-top: 1.5rem; font-size: 0.9rem; color: #718096; }
  .link-btn {
    background: none;
    border: none;
    color: #4299e1;
    cursor: pointer;
    font-weight: 600;
    font-size: 0.9rem;
  }
  .link-btn:hover { text-decoration: underline; }
</style>
