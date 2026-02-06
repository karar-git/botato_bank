<script>
  import { api } from '../lib/api.js';
  import { onMount } from 'svelte';
  import ChatWidget from '../lib/ChatWidget.svelte';

  export let user;

  let accounts = [];
  let selectedAccount = null;
  let transactions = null;
  let loading = true;
  let error = '';
  let success = '';

  // Modal state
  let showDeposit = false;
  let showWithdraw = false;
  let showTransfer = false;
  let showCreateAccount = false;

  // Form state
  let depositAmount = '';
  let depositDesc = '';
  let withdrawAmount = '';
  let withdrawDesc = '';
  let transferDest = '';
  let transferAmount = '';
  let transferDesc = '';
  let newAccountType = 'Savings';

  let actionLoading = false;

  onMount(loadAccounts);

  async function loadAccounts() {
    loading = true;
    error = '';
    try {
      accounts = await api.getAccounts();
      if (accounts.length > 0 && !selectedAccount) {
        selectedAccount = accounts[0];
        await loadTransactions();
      }
    } catch (err) {
      error = err.message || 'Failed to load accounts';
    } finally {
      loading = false;
    }
  }

  async function selectAccount(account) {
    selectedAccount = account;
    await loadTransactions();
  }

  async function loadTransactions() {
    if (!selectedAccount) return;
    try {
      transactions = await api.getTransactions(selectedAccount.id, { page: 1, pageSize: 20 });
    } catch (err) {
      console.error('Failed to load transactions', err);
    }
  }

  function clearMessages() { error = ''; success = ''; }

  async function handleDeposit() {
    actionLoading = true; clearMessages();
    try {
      await api.deposit(selectedAccount.id, {
        amount: parseFloat(depositAmount),
        description: depositDesc || 'Deposit',
        idempotencyKey: crypto.randomUUID()
      });
      success = `Deposited $${parseFloat(depositAmount).toFixed(2)} successfully`;
      showDeposit = false;
      depositAmount = ''; depositDesc = '';
      await loadAccounts();
      selectedAccount = accounts.find(a => a.id === selectedAccount.id);
      await loadTransactions();
    } catch (err) {
      error = err.message || 'Deposit failed';
    } finally { actionLoading = false; }
  }

  async function handleWithdraw() {
    actionLoading = true; clearMessages();
    try {
      await api.withdraw(selectedAccount.id, {
        amount: parseFloat(withdrawAmount),
        description: withdrawDesc || 'Withdrawal',
        idempotencyKey: crypto.randomUUID()
      });
      success = `Withdrew $${parseFloat(withdrawAmount).toFixed(2)} successfully`;
      showWithdraw = false;
      withdrawAmount = ''; withdrawDesc = '';
      await loadAccounts();
      selectedAccount = accounts.find(a => a.id === selectedAccount.id);
      await loadTransactions();
    } catch (err) {
      error = err.message || 'Withdrawal failed';
    } finally { actionLoading = false; }
  }

  async function handleTransfer() {
    actionLoading = true; clearMessages();
    try {
      await api.transfer({
        sourceAccountNumber: selectedAccount.accountNumber,
        destinationAccountNumber: transferDest,
        amount: parseFloat(transferAmount),
        description: transferDesc || 'Transfer',
        idempotencyKey: crypto.randomUUID()
      });
      success = `Transferred $${parseFloat(transferAmount).toFixed(2)} to ${transferDest}`;
      showTransfer = false;
      transferDest = ''; transferAmount = ''; transferDesc = '';
      await loadAccounts();
      selectedAccount = accounts.find(a => a.id === selectedAccount.id);
      await loadTransactions();
    } catch (err) {
      error = err.message || 'Transfer failed';
    } finally { actionLoading = false; }
  }

  async function handleCreateAccount() {
    actionLoading = true; clearMessages();
    try {
      const newAcc = await api.createAccount({ type: newAccountType });
      success = `${newAccountType} account created: ${newAcc.accountNumber}`;
      showCreateAccount = false;
      await loadAccounts();
    } catch (err) {
      error = err.message || 'Failed to create account';
    } finally { actionLoading = false; }
  }

  async function handleReconcile() {
    clearMessages();
    try {
      const result = await api.reconcile(selectedAccount.id);
      if (result.isReconciled) {
        success = `Balance reconciled: Cached=$${result.cachedBalance.toFixed(2)}, Ledger=$${result.ledgerBalance.toFixed(2)} (${result.totalEntries} entries) -- MATCH`;
      } else {
        error = `MISMATCH! Cached=$${result.cachedBalance.toFixed(2)}, Ledger=$${result.ledgerBalance.toFixed(2)}`;
      }
    } catch (err) {
      error = err.message || 'Reconciliation failed';
    }
  }

  function formatDate(d) {
    return new Date(d).toLocaleString();
  }

  function formatAmount(amount) {
    const val = parseFloat(amount);
    const sign = val >= 0 ? '+' : '';
    return `${sign}$${val.toFixed(2)}`;
  }

  function downloadExport(type) {
    if (!selectedAccount) return;
    const token = localStorage.getItem('corebank_token');
    const url = type === 'csv'
      ? api.exportCsv(selectedAccount.id)
      : api.exportXlsx(selectedAccount.id);
    // Open in new tab with auth
    window.open(`${url}?access_token=${token}`, '_blank');
  }
</script>

<div class="dashboard">
  {#if error}
    <div class="alert alert-error">{error} <button on:click={() => error = ''}>x</button></div>
  {/if}
  {#if success}
    <div class="alert alert-success">{success} <button on:click={() => success = ''}>x</button></div>
  {/if}

  {#if loading}
    <div class="loading">Loading your accounts...</div>
  {:else}
    <!-- Accounts sidebar -->
    <div class="layout">
      <aside class="sidebar">
        <div class="sidebar-header">
          <h3>Your Accounts</h3>
          <button class="btn-sm" on:click={() => showCreateAccount = true}>+ New</button>
        </div>
        {#each accounts as account}
          <button
            class="account-card {selectedAccount?.id === account.id ? 'selected' : ''}"
            on:click={() => selectAccount(account)}
          >
            <div class="acc-type">{account.type}</div>
            <div class="acc-number">{account.accountNumber}</div>
            <div class="acc-balance">${account.balance.toFixed(2)}</div>
          </button>
        {/each}
      </aside>

      <!-- Main content -->
      <section class="main-content">
        {#if selectedAccount}
          <div class="account-detail">
            <div class="detail-header">
              <div>
                <h2>{selectedAccount.type} Account</h2>
                <p class="account-num">{selectedAccount.accountNumber}</p>
              </div>
              <div class="balance-display">
                <span class="balance-label">Available Balance</span>
                <span class="balance-value">${selectedAccount.balance.toFixed(2)}</span>
                <span class="balance-currency">{selectedAccount.currency}</span>
              </div>
            </div>

            <div class="actions">
              <button class="btn btn-deposit" on:click={() => { showDeposit = true; clearMessages(); }}>Deposit</button>
              <button class="btn btn-withdraw" on:click={() => { showWithdraw = true; clearMessages(); }}>Withdraw</button>
              <button class="btn btn-transfer" on:click={() => { showTransfer = true; clearMessages(); }}>Transfer</button>
              <button class="btn btn-reconcile" on:click={handleReconcile}>Reconcile</button>
              <button class="btn btn-export" on:click={() => downloadExport('csv')}>CSV</button>
              <button class="btn btn-export" on:click={() => downloadExport('xlsx')}>XLSX</button>
            </div>

            <!-- Transaction History -->
            <div class="tx-section">
              <h3>Transaction History</h3>
              {#if transactions && transactions.items.length > 0}
                <table class="tx-table">
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>Type</th>
                      <th>Amount</th>
                      <th>Balance</th>
                      <th>Description</th>
                    </tr>
                  </thead>
                  <tbody>
                    {#each transactions.items as tx}
                      <tr>
                        <td>{formatDate(tx.createdAt)}</td>
                        <td><span class="badge badge-{tx.type.toLowerCase()}">{tx.type}</span></td>
                        <td class={parseFloat(tx.amount) >= 0 ? 'amount-pos' : 'amount-neg'}>
                          {formatAmount(tx.amount)}
                        </td>
                        <td>${tx.balanceAfter.toFixed(2)}</td>
                        <td class="desc">{tx.description}</td>
                      </tr>
                    {/each}
                  </tbody>
                </table>
                <div class="pagination">
                  Page {transactions.page} of {transactions.totalPages} ({transactions.totalCount} total)
                </div>
              {:else}
                <p class="no-data">No transactions yet. Make a deposit to get started.</p>
              {/if}
            </div>
          </div>
        {:else}
          <p>Select an account to view details.</p>
        {/if}
      </section>
    </div>
  {/if}

  <!-- Modals -->
  {#if showDeposit}
    <div class="modal-backdrop" on:click={() => showDeposit = false}>
      <div class="modal" on:click|stopPropagation>
        <h3>Deposit Funds</h3>
        <form on:submit|preventDefault={handleDeposit}>
          <div class="field">
            <label>Amount ($)</label>
            <input type="number" step="0.01" min="0.01" bind:value={depositAmount} required />
          </div>
          <div class="field">
            <label>Description (optional)</label>
            <input type="text" bind:value={depositDesc} maxlength="500" />
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-cancel" on:click={() => showDeposit = false}>Cancel</button>
            <button type="submit" class="btn btn-deposit" disabled={actionLoading}>
              {actionLoading ? 'Processing...' : 'Deposit'}
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  {#if showWithdraw}
    <div class="modal-backdrop" on:click={() => showWithdraw = false}>
      <div class="modal" on:click|stopPropagation>
        <h3>Withdraw Funds</h3>
        <form on:submit|preventDefault={handleWithdraw}>
          <div class="field">
            <label>Amount ($)</label>
            <input type="number" step="0.01" min="0.01" bind:value={withdrawAmount} required />
          </div>
          <div class="field">
            <label>Description (optional)</label>
            <input type="text" bind:value={withdrawDesc} maxlength="500" />
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-cancel" on:click={() => showWithdraw = false}>Cancel</button>
            <button type="submit" class="btn btn-withdraw" disabled={actionLoading}>
              {actionLoading ? 'Processing...' : 'Withdraw'}
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  {#if showTransfer}
    <div class="modal-backdrop" on:click={() => showTransfer = false}>
      <div class="modal" on:click|stopPropagation>
        <h3>Transfer Funds</h3>
        <form on:submit|preventDefault={handleTransfer}>
          <div class="field">
            <label>From</label>
            <input type="text" value={selectedAccount.accountNumber} disabled />
          </div>
          <div class="field">
            <label>Destination Account Number</label>
            <input type="text" bind:value={transferDest} required placeholder="CHK-20260206-XXXXXX" />
          </div>
          <div class="field">
            <label>Amount ($)</label>
            <input type="number" step="0.01" min="0.01" bind:value={transferAmount} required />
          </div>
          <div class="field">
            <label>Description (optional)</label>
            <input type="text" bind:value={transferDesc} maxlength="500" />
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-cancel" on:click={() => showTransfer = false}>Cancel</button>
            <button type="submit" class="btn btn-transfer" disabled={actionLoading}>
              {actionLoading ? 'Processing...' : 'Transfer'}
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  {#if showCreateAccount}
    <div class="modal-backdrop" on:click={() => showCreateAccount = false}>
      <div class="modal" on:click|stopPropagation>
        <h3>Create New Account</h3>
        <form on:submit|preventDefault={handleCreateAccount}>
          <div class="field">
            <label>Account Type</label>
            <select bind:value={newAccountType}>
              <option value="Checking">Checking</option>
              <option value="Savings">Savings</option>
            </select>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-cancel" on:click={() => showCreateAccount = false}>Cancel</button>
            <button type="submit" class="btn btn-deposit" disabled={actionLoading}>
              {actionLoading ? 'Creating...' : 'Create Account'}
            </button>
          </div>
        </form>
      </div>
    </div>
  {/if}

  <ChatWidget />
</div>

<style>
  .dashboard { position: relative; }
  .layout { display: grid; grid-template-columns: 280px 1fr; gap: 1.5rem; }
  .loading { text-align: center; padding: 3rem; color: #718096; }

  /* Alerts */
  .alert {
    padding: 0.8rem 1rem;
    border-radius: 8px;
    margin-bottom: 1rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 0.9rem;
  }
  .alert button {
    background: none; border: none; cursor: pointer; font-size: 1rem;
    opacity: 0.6; padding: 0 0.3rem;
  }
  .alert-error { background: #fed7d7; color: #c53030; }
  .alert-success { background: #c6f6d5; color: #22543d; }

  /* Sidebar */
  .sidebar {
    background: white;
    border-radius: 12px;
    padding: 1rem;
    box-shadow: 0 2px 10px rgba(0,0,0,0.06);
    height: fit-content;
  }
  .sidebar-header {
    display: flex; justify-content: space-between; align-items: center;
    margin-bottom: 1rem; padding-bottom: 0.5rem; border-bottom: 1px solid #e2e8f0;
  }
  .sidebar-header h3 { font-size: 1rem; color: #2d3748; }
  .btn-sm {
    background: #4299e1; color: white; border: none; padding: 0.3rem 0.7rem;
    border-radius: 4px; cursor: pointer; font-size: 0.8rem;
  }
  .btn-sm:hover { background: #3182ce; }

  .account-card {
    display: block; width: 100%; text-align: left; background: #f7fafc;
    border: 2px solid transparent; border-radius: 8px; padding: 0.8rem;
    margin-bottom: 0.5rem; cursor: pointer; transition: all 0.2s;
  }
  .account-card:hover { border-color: #bee3f8; }
  .account-card.selected { border-color: #4299e1; background: #ebf8ff; }
  .acc-type { font-size: 0.75rem; font-weight: 600; color: #718096; text-transform: uppercase; }
  .acc-number { font-size: 0.8rem; color: #4a5568; margin: 0.2rem 0; font-family: monospace; }
  .acc-balance { font-size: 1.2rem; font-weight: 700; color: #1a1a2e; }

  /* Main */
  .main-content {
    background: white;
    border-radius: 12px;
    padding: 1.5rem;
    box-shadow: 0 2px 10px rgba(0,0,0,0.06);
  }
  .detail-header {
    display: flex; justify-content: space-between; align-items: flex-start;
    padding-bottom: 1rem; border-bottom: 1px solid #e2e8f0; margin-bottom: 1rem;
  }
  .detail-header h2 { font-size: 1.3rem; }
  .account-num { font-family: monospace; color: #718096; font-size: 0.85rem; }
  .balance-display { text-align: right; }
  .balance-label { display: block; font-size: 0.75rem; color: #718096; text-transform: uppercase; }
  .balance-value { font-size: 2rem; font-weight: 700; color: #1a1a2e; }
  .balance-currency { display: block; font-size: 0.8rem; color: #a0aec0; }

  /* Actions */
  .actions { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-bottom: 1.5rem; }
  .btn {
    padding: 0.5rem 1rem; border: none; border-radius: 6px;
    font-size: 0.85rem; font-weight: 600; cursor: pointer; transition: opacity 0.2s;
  }
  .btn:hover { opacity: 0.85; }
  .btn:disabled { opacity: 0.5; cursor: not-allowed; }
  .btn-deposit { background: #48bb78; color: white; }
  .btn-withdraw { background: #ed8936; color: white; }
  .btn-transfer { background: #4299e1; color: white; }
  .btn-reconcile { background: #9f7aea; color: white; }
  .btn-export { background: #718096; color: white; }
  .btn-cancel { background: #e2e8f0; color: #4a5568; }

  /* Transaction Table */
  .tx-section h3 { font-size: 1rem; margin-bottom: 0.8rem; color: #2d3748; }
  .tx-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
  .tx-table th {
    text-align: left; padding: 0.6rem 0.8rem; border-bottom: 2px solid #e2e8f0;
    color: #718096; font-size: 0.75rem; text-transform: uppercase;
  }
  .tx-table td { padding: 0.6rem 0.8rem; border-bottom: 1px solid #f0f2f5; }
  .tx-table tr:hover { background: #f7fafc; }
  .amount-pos { color: #22543d; font-weight: 600; }
  .amount-neg { color: #c53030; font-weight: 600; }
  .desc { color: #718096; max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .no-data { color: #a0aec0; text-align: center; padding: 2rem; }
  .pagination { text-align: center; padding: 0.8rem; color: #718096; font-size: 0.8rem; }

  /* Badge */
  .badge {
    display: inline-block; padding: 0.15rem 0.5rem; border-radius: 12px;
    font-size: 0.7rem; font-weight: 600; text-transform: uppercase;
  }
  .badge-deposit { background: #c6f6d5; color: #22543d; }
  .badge-withdrawal { background: #feebc8; color: #c05621; }
  .badge-transferdebit { background: #fed7d7; color: #c53030; }
  .badge-transfercredit { background: #bee3f8; color: #2b6cb0; }

  /* Modal */
  .modal-backdrop {
    position: fixed; inset: 0; background: rgba(0,0,0,0.4);
    display: flex; justify-content: center; align-items: center; z-index: 100;
  }
  .modal {
    background: white; border-radius: 12px; padding: 2rem;
    width: 90%; max-width: 420px; box-shadow: 0 10px 40px rgba(0,0,0,0.15);
  }
  .modal h3 { margin-bottom: 1.2rem; }
  .field { margin-bottom: 1rem; }
  .field label { display: block; font-size: 0.85rem; font-weight: 600; color: #4a5568; margin-bottom: 0.3rem; }
  .field input, .field select {
    width: 100%; padding: 0.6rem 0.8rem; border: 1px solid #e2e8f0;
    border-radius: 6px; font-size: 0.9rem;
  }
  .field input:focus, .field select:focus { outline: none; border-color: #4299e1; }
  .field input:disabled { background: #f7fafc; color: #a0aec0; }
  .modal-actions { display: flex; gap: 0.5rem; justify-content: flex-end; margin-top: 1.5rem; }

  @media (max-width: 768px) {
    .layout { grid-template-columns: 1fr; }
    .detail-header { flex-direction: column; gap: 1rem; }
    .balance-display { text-align: left; }
  }
</style>
