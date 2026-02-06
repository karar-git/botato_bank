<script>
  import { api, setToken, clearToken, isAuthenticated } from './lib/api.js';
  import Login from './routes/Login.svelte';
  import Dashboard from './routes/Dashboard.svelte';
  import AdminPanel from './routes/AdminPanel.svelte';
  import EmployeePanel from './routes/EmployeePanel.svelte';

  let authenticated = isAuthenticated();
  let user = null;

  async function handleLogin(event) {
    const { token, user: u } = event.detail;
    setToken(token);
    user = u;
    authenticated = true;
  }

  function handleLogout() {
    clearToken();
    user = null;
    authenticated = false;
  }

  // Check auth on mount
  if (authenticated) {
    api.me().then(u => user = u).catch(() => {
      clearToken();
      authenticated = false;
    });
  }
</script>

<main>
  <header>
    <div class="header-content">
      <h1>PotatoBank</h1>
      <span class="subtitle">Digital Banking System</span>
      {#if authenticated && user}
        <div class="user-info">
          <span>{user.name || user.email}</span>
          {#if user.role}
            <span class="role-tag">{user.role}</span>
          {/if}
          <button class="btn-logout" on:click={handleLogout}>Logout</button>
        </div>
      {/if}
    </div>
  </header>

  <div class="container">
    {#if !authenticated}
      <Login on:login={handleLogin} />
    {:else if user && user.role === 'Admin'}
      <AdminPanel {user} />
    {:else if user && user.role === 'Employee'}
      <EmployeePanel {user} />
    {:else if user && user.kycStatus === 'Rejected'}
      <!-- Rejected Screen -->
      <div class="pending-card">
        <div class="pending-icon">&#10060;</div>
        <h2>Identity Verification Failed</h2>
        <p>Your KYC submission was reviewed and rejected by an administrator.</p>
        {#if user.rejectionReason}
          <div class="rejection-notice">
            <strong>Reason:</strong>
            <p>{user.rejectionReason}</p>
          </div>
        {/if}
        <p class="pending-hint">This decision is final. Please contact support if you believe this is an error, or register a new account with valid ID documents.</p>
      </div>
    {:else if user && user.kycStatus !== 'Verified'}
      <!-- Pending KYC Screen -->
      <div class="pending-card">
        <div class="pending-icon">&#9203;</div>
        <h2>Account Pending KYC Review</h2>
        <p>Your account registration has been received. An administrator will review your ID documents and verify your identity shortly.</p>
        <p class="pending-hint">You will be able to access banking features once your KYC status is verified.</p>
        <button class="btn-refresh" on:click={() => api.me().then(u => user = u)}>Check Status</button>
      </div>
    {:else if user}
      <Dashboard {user} />
    {:else}
      <div class="loading">Loading...</div>
    {/if}
  </div>
</main>

<style>
  :global(*) {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
  }
  :global(body) {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    background: #f0f2f5;
    color: #1a1a2e;
    line-height: 1.6;
  }
  main {
    min-height: 100vh;
  }
  header {
    background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
    color: white;
    padding: 1rem 2rem;
    box-shadow: 0 2px 10px rgba(0,0,0,0.2);
  }
  .header-content {
    max-width: 1200px;
    margin: 0 auto;
    display: flex;
    align-items: center;
    gap: 1rem;
  }
  h1 { font-size: 1.5rem; font-weight: 700; }
  .subtitle {
    color: #a0aec0;
    font-size: 0.85rem;
    border-left: 1px solid #4a5568;
    padding-left: 1rem;
  }
  .user-info {
    margin-left: auto;
    display: flex;
    align-items: center;
    gap: 1rem;
    font-size: 0.9rem;
  }
  .role-tag {
    background: rgba(255,255,255,0.15);
    padding: 0.15rem 0.6rem;
    border-radius: 12px;
    font-size: 0.7rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }
  .btn-logout {
    background: transparent;
    border: 1px solid #e53e3e;
    color: #fc8181;
    padding: 0.3rem 0.8rem;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.8rem;
  }
  .btn-logout:hover { background: #e53e3e; color: white; }
  .container {
    max-width: 1200px;
    margin: 2rem auto;
    padding: 0 1rem;
  }
  .loading {
    text-align: center;
    padding: 3rem;
    color: #718096;
  }

  /* Pending Approval */
  .pending-card {
    background: white;
    border-radius: 12px;
    padding: 3rem;
    max-width: 520px;
    margin: 3rem auto;
    text-align: center;
    box-shadow: 0 4px 20px rgba(0,0,0,0.08);
  }
  .pending-icon {
    font-size: 3rem;
    margin-bottom: 1rem;
  }
  .pending-card h2 {
    font-size: 1.4rem;
    margin-bottom: 1rem;
    color: #1a1a2e;
  }
  .pending-card p {
    color: #718096;
    font-size: 0.95rem;
    margin-bottom: 0.5rem;
  }
  .pending-hint {
    font-size: 0.85rem;
    color: #a0aec0;
    margin-top: 0.5rem;
  }
  .rejection-notice {
    background: #fed7d7;
    color: #c53030;
    padding: 1rem;
    border-radius: 8px;
    margin-top: 1.5rem;
    text-align: left;
    font-size: 0.85rem;
  }
  .rejection-notice strong {
    display: block;
    margin-bottom: 0.3rem;
  }
  .rejection-notice p {
    color: #c53030;
    margin-bottom: 0.3rem;
  }
  .btn-refresh {
    margin-top: 1.5rem;
    background: #4299e1;
    color: white;
    border: none;
    padding: 0.6rem 1.5rem;
    border-radius: 6px;
    font-size: 0.9rem;
    font-weight: 600;
    cursor: pointer;
    transition: opacity 0.2s;
  }
  .btn-refresh:hover { opacity: 0.85; }
</style>
