<script>
  import { api, setToken, clearToken, isAuthenticated } from './lib/api.js';
  import Login from './routes/Login.svelte';
  import Home from './routes/Home.svelte';
  import Dashboard from './routes/Dashboard.svelte';
  import AdminPanel from './routes/AdminPanel.svelte';
  import EmployeePanel from './routes/EmployeePanel.svelte';

  let authenticated = isAuthenticated();
  let user = null;
  let switchingRole = false;
  let page = 'home'; // 'home', 'login', 'register'
  let loading = true;

  const allRoles = ['Customer', 'Merchant', 'Employee', 'Admin'];

  function navigate(target) {
    page = target;
  }

  async function handleLogin(event) {
    const { token, user: u } = event.detail;
    setToken(token);
    user = u;
    authenticated = true;
  }

  async function handleRoleSwitch(event) {
    const newRole = event.target.value;
    if (!newRole || newRole === user.role) return;
    switchingRole = true;
    try {
      const res = await api.switchRole(newRole);
      setToken(res.token);
      user = res.user;
    } catch (err) {
      alert('Failed to switch role: ' + (err.message || err));
      event.target.value = user.role;
    } finally {
      switchingRole = false;
    }
  }

  function handleLogout() {
    clearToken();
    user = null;
    authenticated = false;
    page = 'home';
  }

  // Check auth on mount
  if (authenticated) {
    api.me().then(u => {
      user = u;
      loading = false;
    }).catch(() => {
      clearToken();
      authenticated = false;
      loading = false;
    });
  } else {
    loading = false;
  }
</script>

{#if !authenticated}
  <!-- UNAUTHENTICATED: Home / Login / Register -->
  {#if page === 'home'}
    <Home {navigate} />
  {:else}
    <Login initialMode={page} {navigate} on:login={handleLogin} />
  {/if}

{:else if loading}
  <!-- LOADING -->
  <div class="loading-screen">
    <div class="atom atom-1"></div>
    <div class="atom atom-2"></div>
    <div class="atom atom-3"></div>
    <div class="spinner"></div>
    <p class="loading-text">Loading your account...</p>
  </div>

{:else if user && (user.role === 'Admin' || user.role === 'Employee' || user.kycStatus === 'Verified')}
  <!-- AUTHENTICATED APP SHELL -->
  <div class="app-shell">
    <header class="app-header">
      <div class="header-inner">
        <div class="logo-area">
          <h1 class="logo">
            <span class="logo-highlight">Atomic</span> Bank
          </h1>
        </div>
        <div class="user-controls">
          <span class="user-name">{user.name || user.email}</span>
          <span class="role-tag">{user.role}</span>
          <select
            class="role-switcher"
            value={user.role}
            on:change={handleRoleSwitch}
            disabled={switchingRole}
          >
            {#each allRoles as role}
              <option value={role}>{role}</option>
            {/each}
          </select>
          <button class="btn-logout" on:click={handleLogout}>Logout</button>
        </div>
      </div>
    </header>

    <div class="app-content">
      {#if user.role === 'Admin'}
        <AdminPanel {user} />
      {:else if user.role === 'Employee'}
        <EmployeePanel {user} />
      {:else}
        <Dashboard {user} />
      {/if}
    </div>
  </div>

{:else if user && user.kycStatus === 'Rejected'}
  <!-- KYC REJECTED -->
  <div class="kyc-screen">
    <div class="atom atom-1"></div>
    <div class="atom atom-2"></div>
    <div class="atom atom-3"></div>
    <div class="atom atom-4"></div>
    <div class="atom atom-5"></div>
    <div class="kyc-card">
      <div class="kyc-icon rejected-icon">&#10060;</div>
      <h2>Identity Verification Failed</h2>
      <p>Your KYC submission was reviewed and rejected by an administrator.</p>
      {#if user.rejectionReason}
        <div class="rejection-notice">
          <strong>Reason:</strong>
          <p>{user.rejectionReason}</p>
        </div>
      {/if}
      <p class="kyc-hint">This decision is final. Please contact support if you believe this is an error, or register a new account with valid ID documents.</p>
      <button class="btn-signout" on:click={handleLogout}>Sign Out</button>
    </div>
  </div>

{:else if user}
  <!-- KYC PENDING -->
  <div class="kyc-screen">
    <div class="atom atom-1"></div>
    <div class="atom atom-2"></div>
    <div class="atom atom-3"></div>
    <div class="atom atom-4"></div>
    <div class="atom atom-5"></div>
    <div class="kyc-card">
      <div class="kyc-icon pending-icon">&#9203;</div>
      <h2>Account Pending KYC Review</h2>
      <p>Your account registration has been received. An administrator will review your ID documents and verify your identity shortly.</p>
      <p class="kyc-hint">You will be able to access banking features once your KYC status is verified.</p>
      <button class="btn-check" on:click={() => api.me().then(u => user = u)}>Check Status</button>
      <button class="btn-signout" on:click={handleLogout}>Sign Out</button>
    </div>
  </div>

{:else}
  <!-- FALLBACK LOADING -->
  <div class="loading-screen">
    <div class="spinner"></div>
    <p class="loading-text">Loading...</p>
  </div>
{/if}

<style>
  /* ==================== APP SHELL ==================== */
  .app-shell {
    min-height: 100vh;
    background: var(--bg-secondary, #ECF0F1);
  }

  .app-header {
    background: rgba(255, 255, 255, 0.9);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border-bottom: 1px solid #e2e8f0;
    padding: 0.8rem 2rem;
    position: sticky;
    top: 0;
    z-index: 100;
    box-shadow: 0 1px 8px rgba(0, 0, 0, 0.04);
  }

  .header-inner {
    max-width: 1400px;
    margin: 0 auto;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  .logo {
    font-size: 1.4rem;
    font-weight: 700;
    color: var(--brand-dark, #34495E);
    margin: 0;
    letter-spacing: -0.5px;
  }

  .logo-highlight {
    background: linear-gradient(135deg, var(--brand-primary, #3498DB) 0%, var(--accent, #F8C957) 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
  }

  .user-controls {
    display: flex;
    align-items: center;
    gap: 0.8rem;
  }

  .user-name {
    color: var(--text-main, #2c3e50);
    font-size: 0.9rem;
    font-weight: 500;
  }

  .role-tag {
    background: rgba(52, 152, 219, 0.12);
    color: var(--brand-primary, #3498DB);
    padding: 0.15rem 0.7rem;
    border-radius: 20px;
    font-size: 0.7rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    border: 1px solid rgba(52, 152, 219, 0.2);
  }

  .role-switcher {
    background: white;
    color: var(--text-main, #2c3e50);
    border: 1px solid #e2e8f0;
    padding: 0.3rem 0.6rem;
    border-radius: 10px;
    font-size: 0.8rem;
    font-family: inherit;
    cursor: pointer;
    outline: none;
    transition: all 0.2s ease;
  }
  .role-switcher:hover { border-color: #cbd5e0; }
  .role-switcher:focus { border-color: var(--brand-primary, #3498DB); box-shadow: 0 0 0 3px rgba(52, 152, 219, 0.15); }
  .role-switcher:disabled { opacity: 0.5; cursor: wait; }
  .role-switcher option {
    background: white;
    color: var(--text-main, #2c3e50);
  }

  .btn-logout {
    background: transparent;
    border: 1px solid rgba(229, 62, 62, 0.4);
    color: #e53e3e;
    padding: 0.3rem 1rem;
    border-radius: 20px;
    cursor: pointer;
    font-size: 0.8rem;
    font-family: inherit;
    transition: all 0.2s ease;
  }
  .btn-logout:hover {
    background: rgba(229, 62, 62, 0.08);
    border-color: #e53e3e;
    color: #c53030;
  }

  .app-content {
    max-width: 1400px;
    margin: 0 auto;
    padding: 1.5rem;
  }

  /* ==================== LOADING SCREEN ==================== */
  .loading-screen {
    min-height: 100vh;
    background: var(--bg-secondary, #ECF0F1);
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    position: relative;
    overflow: hidden;
  }

  .spinner {
    width: 40px;
    height: 40px;
    border: 3px solid #e2e8f0;
    border-top-color: var(--brand-primary, #3498DB);
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
  }

  .loading-text {
    color: var(--text-muted, #7f8c8d);
    font-size: 0.9rem;
    margin-top: 1rem;
  }

  @keyframes spin {
    to { transform: rotate(360deg); }
  }

  /* ==================== KYC SCREENS ==================== */
  .kyc-screen {
    min-height: 100vh;
    background: var(--bg-secondary, #ECF0F1);
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
    overflow: hidden;
    padding: 2rem;
  }

  .kyc-card {
    background: rgba(255, 255, 255, 0.85);
    backdrop-filter: blur(25px);
    -webkit-backdrop-filter: blur(25px);
    border: 1px solid rgba(255, 255, 255, 0.6);
    border-radius: 20px;
    padding: 3rem 2.5rem;
    max-width: 500px;
    width: 100%;
    text-align: center;
    position: relative;
    z-index: 1;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.08);
  }

  .kyc-icon {
    font-size: 3.5rem;
    margin-bottom: 1.2rem;
  }

  .kyc-card h2 {
    color: var(--brand-dark, #34495E);
    font-size: 1.4rem;
    font-weight: 700;
    margin-bottom: 1rem;
    letter-spacing: -0.3px;
  }

  .kyc-card p {
    color: var(--text-muted, #7f8c8d);
    font-size: 0.95rem;
    line-height: 1.6;
    margin-bottom: 0.5rem;
  }

  .kyc-hint {
    font-size: 0.85rem !important;
    color: #b0bec5 !important;
    margin-top: 0.5rem;
  }

  .rejection-notice {
    background: #fef2f2;
    border: 1px solid #fecaca;
    color: #991b1b;
    padding: 1rem 1.2rem;
    border-radius: 12px;
    margin-top: 1.5rem;
    text-align: left;
    font-size: 0.85rem;
  }
  .rejection-notice strong {
    display: block;
    margin-bottom: 0.3rem;
    color: #dc2626;
  }
  .rejection-notice p {
    color: #991b1b !important;
    margin-bottom: 0 !important;
  }

  .btn-check {
    margin-top: 1.5rem;
    background: var(--brand-dark, #34495E);
    color: white;
    border: none;
    padding: 0.7rem 2rem;
    border-radius: 50px;
    font-size: 0.9rem;
    font-weight: 600;
    font-family: inherit;
    cursor: pointer;
    transition: all 0.3s ease;
  }
  .btn-check:hover {
    transform: scale(1.03);
    background: var(--brand-primary, #3498DB);
    box-shadow: 0 4px 20px rgba(52, 152, 219, 0.3);
  }

  .btn-signout {
    margin-top: 1rem;
    background: transparent;
    border: 1px solid #e2e8f0;
    color: var(--text-muted, #7f8c8d);
    padding: 0.5rem 1.5rem;
    border-radius: 50px;
    font-size: 0.85rem;
    font-family: inherit;
    cursor: pointer;
    display: block;
    margin-left: auto;
    margin-right: auto;
    transition: all 0.2s ease;
  }
  .btn-signout:hover {
    border-color: #cbd5e0;
    color: var(--text-main, #2c3e50);
  }

  /* ==================== FLOATING ATOMS ==================== */
  .atom {
    position: absolute;
    border-radius: 50%;
    filter: blur(100px);
    opacity: 0.2;
    z-index: 0;
    pointer-events: none;
    animation: floatAtom 15s infinite ease-in-out alternate;
  }
  .atom-1 {
    width: 400px; height: 400px;
    background: var(--brand-primary, #3498DB);
    top: -10%; left: -10%;
    animation-delay: 0s;
  }
  .atom-2 {
    width: 500px; height: 500px;
    background: #85c1e9;
    bottom: 10%; right: -10%;
    animation-delay: -5s;
  }
  .atom-3 {
    width: 300px; height: 300px;
    background: var(--accent, #F8C957);
    top: 40%; left: 30%;
    opacity: 0.12;
    animation-delay: -10s;
  }
  .atom-4 {
    width: 200px; height: 200px;
    background: #aed6f1;
    top: 15%; right: 15%;
    opacity: 0.15;
    animation-delay: -2s;
  }
  .atom-5 {
    width: 350px; height: 350px;
    background: var(--accent, #F8C957);
    bottom: 5%; left: 10%;
    opacity: 0.1;
    animation-delay: -7s;
  }

  @keyframes floatAtom {
    0% { transform: translate(0, 0) scale(1); }
    100% { transform: translate(30px, -30px) scale(1.1); }
  }

  /* ==================== RESPONSIVE ==================== */
  @media (max-width: 768px) {
    .header-inner {
      flex-direction: column;
      gap: 0.6rem;
    }
    .user-controls {
      flex-wrap: wrap;
      justify-content: center;
    }
    .app-header {
      padding: 0.6rem 1rem;
    }
    .app-content {
      padding: 1rem 0.5rem;
    }
  }
</style>
