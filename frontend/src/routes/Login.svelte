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
  let idCardFront = null;
  let idCardFrontName = '';
  let nationalIdNumber = '';
  let idCardBack = null;
  let idCardBackName = '';

  function handleFrontChange(e) {
    const file = e.target.files[0];
    if (file) {
      idCardFront = file;
      idCardFrontName = file.name;
    }
  }

  function handleBackChange(e) {
    const file = e.target.files[0];
    if (file) {
      idCardBack = file;
      idCardBackName = file.name;
    }
  }

  async function handleSubmit() {
    loading = true;
    error = '';
    try {
      let result;
      if (mode === 'register') {
        if (!idCardFront || !idCardBack) {
          error = 'Please upload both front and back photos of your national ID card (البطاقة الوطنية)';
          loading = false;
          return;
        }
        result = await api.register({ fullName, email, password, idCardFront, idCardBack, nationalIdNumber });
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
        <div class="field">
          <label for="nationalIdNumber">National ID Number (رقم البطاقة الوطنية)</label>
          <input id="nationalIdNumber" type="text" bind:value={nationalIdNumber} required maxlength="50" placeholder="e.g. AB123456" />
          <p class="field-hint">Your national ID number is used for identity verification and payroll processing.</p>
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

      {#if mode === 'register'}
        <div class="field">
          <label for="idCardFront">ID Card - Front (الوجه الأمامي)</label>
          <div class="file-upload" class:has-file={idCardFrontName}>
            <input id="idCardFront" type="file" accept="image/jpeg,image/png,image/webp" on:change={handleFrontChange} required />
            <div class="file-upload-label">
              {#if idCardFrontName}
                <span class="file-name">{idCardFrontName}</span>
              {:else}
                <span class="file-placeholder">Upload front of your ID card</span>
              {/if}
            </div>
          </div>
        </div>

        <div class="field">
          <label for="idCardBack">ID Card - Back (الوجه الخلفي)</label>
          <div class="file-upload" class:has-file={idCardBackName}>
            <input id="idCardBack" type="file" accept="image/jpeg,image/png,image/webp" on:change={handleBackChange} required />
            <div class="file-upload-label">
              {#if idCardBackName}
                <span class="file-name">{idCardBackName}</span>
              {:else}
                <span class="file-placeholder">Upload back of your ID card</span>
              {/if}
            </div>
          </div>
          <p class="field-hint">An admin will review your ID for verification before activating your account. Your KYC status must be verified to perform banking operations.</p>
        </div>
      {/if}

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
  .file-upload {
    position: relative;
    border: 2px dashed #e2e8f0;
    border-radius: 6px;
    padding: 1rem;
    text-align: center;
    cursor: pointer;
    transition: border-color 0.2s;
  }
  .file-upload:hover, .file-upload.has-file { border-color: #4299e1; }
  .file-upload input[type="file"] {
    position: absolute;
    inset: 0;
    opacity: 0;
    cursor: pointer;
    width: 100%;
    height: 100%;
  }
  .file-placeholder { color: #a0aec0; font-size: 0.9rem; }
  .file-name { color: #2d3748; font-weight: 600; font-size: 0.9rem; }
  .field-hint { font-size: 0.75rem; color: #a0aec0; margin-top: 0.3rem; }
</style>
