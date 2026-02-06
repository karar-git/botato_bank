<script>
  import { api, setToken, clearToken, isAuthenticated } from './lib/api.js';
  import Login from './routes/Login.svelte';
  import Dashboard from './routes/Dashboard.svelte';

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
      <h1>CoreBank</h1>
      <span class="subtitle">Digital Banking System</span>
      {#if authenticated && user}
        <div class="user-info">
          <span>{user.name || user.email}</span>
          <button class="btn-logout" on:click={handleLogout}>Logout</button>
        </div>
      {/if}
    </div>
  </header>

  <div class="container">
    {#if !authenticated}
      <Login on:login={handleLogin} />
    {:else}
      <Dashboard {user} />
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
</style>
