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
  let showCreateCard = false;
  let showCards = false;

  // Form state
  let depositAmount = '';
  let depositDesc = '';
  let withdrawAmount = '';
  let withdrawDesc = '';
  let withdrawAtm = '';
  let transferDest = '';
  let transferAmount = '';
  let transferDesc = '';
  let newAccountType = 'Savings';
  let cardDailyLimit = '5000';

  // Debit cards
  let cards = [];
  let newCardDetails = null; // shown once after creation
  let cardsLoading = false;

  let actionLoading = false;

  onMount(() => {
    loadAccounts();
    loadCards();
  });

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
        atmNumber: withdrawAtm,
        idempotencyKey: crypto.randomUUID()
      });
      success = `Withdrew $${parseFloat(withdrawAmount).toFixed(2)} successfully (ATM: ${withdrawAtm})`;
      showWithdraw = false;
      withdrawAmount = ''; withdrawDesc = ''; withdrawAtm = '';
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

  async function loadCards() {
    cardsLoading = true;
    try {
      cards = await api.getCards();
    } catch (err) {
      console.error('Failed to load cards', err);
    } finally {
      cardsLoading = false;
    }
  }

  async function handleCreateCard() {
    actionLoading = true; clearMessages();
    try {
      const result = await api.createCard({
        accountId: selectedAccount.id,
        dailyLimit: parseFloat(cardDailyLimit)
      });
      newCardDetails = result;
      success = 'Debit card issued successfully! Save your card details now — the CVV will not be shown again.';
      showCreateCard = false;
      cardDailyLimit = '5000';
      await loadCards();
    } catch (err) {
      error = err.message || 'Failed to create debit card';
    } finally { actionLoading = false; }
  }

  async function handleCardStatusChange(cardId, newStatus) {
    clearMessages();
    try {
      await api.updateCardStatus(cardId, newStatus);
      success = `Card ${newStatus.toLowerCase()} successfully`;
      await loadCards();
    } catch (err) {
      error = err.message || 'Failed to update card status';
    }
  }

  function getCardForAccount(accountId) {
    return cards.find(c => c.accountId === accountId && c.status !== 'Cancelled');
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
            class="account-card {selectedAccount?.id === account.id ? 'selected' : ''} type-{account.type.toLowerCase()}"
            on:click={() => selectAccount(account)}
          >
            <div class="acc-type-row">
              <span class="acc-type-icon">
                {#if account.type === 'Checking'}&#128179;{:else if account.type === 'Savings'}&#128176;{:else}&#127970;{/if}
              </span>
              <span class="acc-type">{account.type}</span>
            </div>
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

            <!-- Account type info -->
            {#if selectedAccount.type === 'Savings'}
              <div class="account-type-info savings-info">Savings accounts earn interest. Outgoing transfers are not permitted.</div>
            {:else if selectedAccount.type === 'Business'}
              <div class="account-type-info business-info">Business account for merchant operations. Higher transaction limits.</div>
            {:else}
              <div class="account-type-info checking-info">Standard checking account with full banking operations.</div>
            {/if}

            <div class="actions">
              {#if user.role === 'Merchant'}
                <button class="btn btn-deposit" on:click={() => { showDeposit = true; clearMessages(); }}>Deposit</button>
              {/if}
              <button class="btn btn-withdraw" on:click={() => { showWithdraw = true; clearMessages(); }}>Withdraw</button>
              {#if selectedAccount.type !== 'Savings'}
                <button class="btn btn-transfer" on:click={() => { showTransfer = true; clearMessages(); }}>Transfer</button>
              {/if}
              {#if !getCardForAccount(selectedAccount.id)}
                <button class="btn btn-card" on:click={() => { showCreateCard = true; clearMessages(); }}>Issue Card</button>
              {/if}
              <button class="btn btn-reconcile" on:click={handleReconcile}>Reconcile</button>
              <button class="btn btn-export" on:click={() => downloadExport('csv')}>CSV</button>
              <button class="btn btn-export" on:click={() => downloadExport('xlsx')}>XLSX</button>
            </div>

            <!-- Debit Card for this account -->
            {#if getCardForAccount(selectedAccount.id)}
              {@const card = getCardForAccount(selectedAccount.id)}
              <div class="card-section">
                <div class="debit-card {card.status.toLowerCase()}">
                  <div class="card-chip"></div>
                  <div class="card-number">{card.maskedCardNumber}</div>
                  <div class="card-bottom">
                    <div class="card-holder">
                      <span class="card-label">CARDHOLDER</span>
                      <span class="card-value">{card.cardholderName}</span>
                    </div>
                    <div class="card-expiry">
                      <span class="card-label">EXPIRES</span>
                      <span class="card-value">{card.expiryDate}</span>
                    </div>
                    <div class="card-status-badge status-{card.status.toLowerCase()}">{card.status}</div>
                  </div>
                  <div class="card-limit">Daily limit: ${card.dailyLimit.toFixed(0)}</div>
                </div>
                <div class="card-actions">
                  {#if card.status === 'Active'}
                    <button class="btn btn-freeze" on:click={() => handleCardStatusChange(card.id, 'Frozen')}>Freeze Card</button>
                  {:else if card.status === 'Frozen'}
                    <button class="btn btn-unfreeze" on:click={() => handleCardStatusChange(card.id, 'Active')}>Unfreeze</button>
                  {/if}
                  {#if card.status !== 'Cancelled'}
                    <button class="btn btn-cancel-card" on:click={() => handleCardStatusChange(card.id, 'Cancelled')}>Cancel Card</button>
                  {/if}
                </div>
              </div>
            {/if}

            <!-- New Card Details (shown once) -->
            {#if newCardDetails && newCardDetails.accountId === selectedAccount.id}
              <div class="new-card-reveal">
                <h4>Your New Card Details</h4>
                <p class="card-warning">Save these details now. The CVV will NOT be shown again.</p>
                <div class="card-details-grid">
                  <div><span class="detail-label">Card Number</span><span class="detail-value mono">{newCardDetails.cardNumber}</span></div>
                  <div><span class="detail-label">Cardholder</span><span class="detail-value">{newCardDetails.cardholderName}</span></div>
                  <div><span class="detail-label">Expiry</span><span class="detail-value">{newCardDetails.expiryDate}</span></div>
                  <div><span class="detail-label">CVV</span><span class="detail-value cvv-highlight">{newCardDetails.cvv}</span></div>
                </div>
                <button class="btn btn-dismiss" on:click={() => newCardDetails = null}>I've saved my details</button>
              </div>
            {/if}

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
            <label>ATM / Branch Number</label>
            <input type="text" bind:value={withdrawAtm} required placeholder="e.g. ATM-001 or BRANCH-MAIN" maxlength="50" />
          </div>
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
              <option value="Checking">Checking - Full banking operations</option>
              <option value="Savings">Savings - Deposit & withdraw only</option>
              {#if user.role === 'Merchant'}
                <option value="Business">Business - Merchant operations</option>
              {/if}
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

  {#if showCreateCard}
    <div class="modal-backdrop" on:click={() => showCreateCard = false}>
      <div class="modal" on:click|stopPropagation>
        <h3>Issue Debit Card</h3>
        <p class="modal-subtitle">A virtual debit card will be issued for your {selectedAccount.type} account ({selectedAccount.accountNumber}).</p>
        <form on:submit|preventDefault={handleCreateCard}>
          <div class="field">
            <label>Daily Spending Limit ($)</label>
            <input type="number" step="100" min="100" max="100000" bind:value={cardDailyLimit} required />
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-cancel" on:click={() => showCreateCard = false}>Cancel</button>
            <button type="submit" class="btn btn-card" disabled={actionLoading}>
              {actionLoading ? 'Issuing...' : 'Issue Card'}
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
  .loading { text-align: center; padding: 3rem; color: rgba(255, 255, 255, 0.5); }

  /* Alerts */
  .alert {
    padding: 0.8rem 1rem;
    border-radius: 12px;
    margin-bottom: 1rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 0.9rem;
    backdrop-filter: blur(10px);
  }
  .alert button {
    background: none; border: none; cursor: pointer; font-size: 1rem;
    opacity: 0.6; padding: 0 0.3rem; color: inherit;
  }
  .alert-error { background: rgba(229, 62, 62, 0.15); color: #feb2b2; border: 1px solid rgba(229, 62, 62, 0.25); }
  .alert-success { background: rgba(72, 187, 120, 0.15); color: #9ae6b4; border: 1px solid rgba(72, 187, 120, 0.25); }

  /* Sidebar */
  .sidebar {
    background: rgba(255, 255, 255, 0.03);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 16px;
    padding: 1rem;
    height: fit-content;
  }
  .sidebar-header {
    display: flex; justify-content: space-between; align-items: center;
    margin-bottom: 1rem; padding-bottom: 0.5rem; border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  }
  .sidebar-header h3 { font-size: 1rem; color: #fff; }
  .btn-sm {
    background: linear-gradient(135deg, rgba(0, 108, 104, 0.8), rgba(41, 161, 156, 0.6));
    color: white; border: none; padding: 0.3rem 0.7rem;
    border-radius: 20px; cursor: pointer; font-size: 0.8rem; font-family: inherit;
    transition: all 0.2s ease;
  }
  .btn-sm:hover { transform: scale(1.05); box-shadow: 0 2px 10px rgba(41, 161, 156, 0.3); }

  .account-card {
    display: block; width: 100%; text-align: left;
    background: rgba(255, 255, 255, 0.03);
    border: 1px solid rgba(255, 255, 255, 0.08); border-radius: 12px; padding: 0.8rem;
    margin-bottom: 0.5rem; cursor: pointer; transition: all 0.2s; color: #fff;
    font-family: inherit;
  }
  .account-card:hover { border-color: rgba(41, 161, 156, 0.4); background: rgba(255, 255, 255, 0.05); }
  .account-card.selected { border-color: #29a19c; background: rgba(41, 161, 156, 0.1); }
  .acc-type { font-size: 0.75rem; font-weight: 600; color: rgba(255, 255, 255, 0.5); text-transform: uppercase; }
  .acc-type-row { display: flex; align-items: center; gap: 0.3rem; }
  .acc-type-icon { font-size: 1rem; }
  .acc-number { font-size: 0.8rem; color: rgba(255, 255, 255, 0.4); margin: 0.2rem 0; font-family: monospace; }
  .acc-balance { font-size: 1.2rem; font-weight: 700; color: #fff; }
  .type-checking.selected { border-color: #29a19c; background: rgba(41, 161, 156, 0.1); }
  .type-savings.selected { border-color: #a3f7bf; background: rgba(163, 247, 191, 0.08); }
  .type-business.selected { border-color: #fdff84; background: rgba(253, 255, 132, 0.06); }

  /* Account type info banner */
  .account-type-info {
    font-size: 0.8rem;
    padding: 0.5rem 0.8rem;
    border-radius: 8px;
    margin-bottom: 1rem;
  }
  .checking-info { background: rgba(41, 161, 156, 0.1); color: rgba(163, 247, 191, 0.9); border-left: 3px solid #29a19c; }
  .savings-info { background: rgba(163, 247, 191, 0.08); color: rgba(163, 247, 191, 0.9); border-left: 3px solid #a3f7bf; }
  .business-info { background: rgba(253, 255, 132, 0.06); color: rgba(253, 255, 132, 0.9); border-left: 3px solid #fdff84; }

  /* Main */
  .main-content {
    background: rgba(255, 255, 255, 0.03);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border: 1px solid rgba(255, 255, 255, 0.08);
    border-radius: 16px;
    padding: 1.5rem;
  }
  .detail-header {
    display: flex; justify-content: space-between; align-items: flex-start;
    padding-bottom: 1rem; border-bottom: 1px solid rgba(255, 255, 255, 0.08); margin-bottom: 1rem;
  }
  .detail-header h2 { font-size: 1.3rem; color: #fff; }
  .account-num { font-family: monospace; color: rgba(255, 255, 255, 0.4); font-size: 0.85rem; }
  .balance-display { text-align: right; }
  .balance-label { display: block; font-size: 0.75rem; color: rgba(255, 255, 255, 0.5); text-transform: uppercase; }
  .balance-value {
    font-size: 2rem; font-weight: 700;
    background: linear-gradient(135deg, #fdff84 0%, #a3f7bf 50%, #29a19c 100%);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
  }
  .balance-currency { display: block; font-size: 0.8rem; color: rgba(255, 255, 255, 0.35); }

  /* Actions */
  .actions { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-bottom: 1.5rem; }
  .btn {
    padding: 0.5rem 1rem; border: none; border-radius: 20px;
    font-size: 0.85rem; font-weight: 600; cursor: pointer; transition: all 0.2s ease;
    font-family: inherit;
  }
  .btn:hover { transform: scale(1.03); }
  .btn:disabled { opacity: 0.5; cursor: not-allowed; transform: none; }
  .btn-deposit { background: rgba(72, 187, 120, 0.2); color: #9ae6b4; border: 1px solid rgba(72, 187, 120, 0.3); }
  .btn-deposit:hover { background: rgba(72, 187, 120, 0.3); }
  .btn-withdraw { background: rgba(237, 137, 54, 0.2); color: #fbd38d; border: 1px solid rgba(237, 137, 54, 0.3); }
  .btn-withdraw:hover { background: rgba(237, 137, 54, 0.3); }
  .btn-transfer { background: rgba(41, 161, 156, 0.2); color: #a3f7bf; border: 1px solid rgba(41, 161, 156, 0.3); }
  .btn-transfer:hover { background: rgba(41, 161, 156, 0.3); }
  .btn-card { background: rgba(102, 126, 234, 0.2); color: #b794f4; border: 1px solid rgba(102, 126, 234, 0.3); }
  .btn-card:hover { background: rgba(102, 126, 234, 0.3); }
  .btn-reconcile { background: rgba(159, 122, 234, 0.2); color: #d6bcfa; border: 1px solid rgba(159, 122, 234, 0.3); }
  .btn-reconcile:hover { background: rgba(159, 122, 234, 0.3); }
  .btn-export { background: rgba(255, 255, 255, 0.08); color: rgba(255, 255, 255, 0.7); border: 1px solid rgba(255, 255, 255, 0.12); }
  .btn-export:hover { background: rgba(255, 255, 255, 0.12); }
  .btn-cancel { background: rgba(255, 255, 255, 0.05); color: rgba(255, 255, 255, 0.6); border: 1px solid rgba(255, 255, 255, 0.1); }
  .btn-freeze { background: rgba(49, 130, 206, 0.2); color: #90cdf4; border: 1px solid rgba(49, 130, 206, 0.3); }
  .btn-unfreeze { background: rgba(72, 187, 120, 0.2); color: #9ae6b4; border: 1px solid rgba(72, 187, 120, 0.3); }
  .btn-cancel-card { background: rgba(229, 62, 62, 0.2); color: #feb2b2; border: 1px solid rgba(229, 62, 62, 0.3); }
  .btn-dismiss { background: linear-gradient(135deg, rgba(0, 108, 104, 0.8), rgba(41, 161, 156, 0.6)); color: white; border: none; margin-top: 1rem; }

  /* Transaction Table */
  .tx-section h3 { font-size: 1rem; margin-bottom: 0.8rem; color: #fff; }
  .tx-table { width: 100%; border-collapse: collapse; font-size: 0.85rem; color: rgba(255, 255, 255, 0.8); }
  .tx-table th {
    text-align: left; padding: 0.6rem 0.8rem; border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    color: rgba(255, 255, 255, 0.45); font-size: 0.75rem; text-transform: uppercase;
  }
  .tx-table td { padding: 0.6rem 0.8rem; border-bottom: 1px solid rgba(255, 255, 255, 0.04); }
  .tx-table tr:hover { background: rgba(255, 255, 255, 0.03); }
  .amount-pos { color: #a3f7bf; font-weight: 600; }
  .amount-neg { color: #feb2b2; font-weight: 600; }
  .desc { color: rgba(255, 255, 255, 0.45); max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .no-data { color: rgba(255, 255, 255, 0.35); text-align: center; padding: 2rem; }
  .pagination { text-align: center; padding: 0.8rem; color: rgba(255, 255, 255, 0.4); font-size: 0.8rem; }

  /* Badge */
  .badge {
    display: inline-block; padding: 0.15rem 0.5rem; border-radius: 12px;
    font-size: 0.7rem; font-weight: 600; text-transform: uppercase;
  }
  .badge-deposit { background: rgba(72, 187, 120, 0.15); color: #9ae6b4; }
  .badge-withdrawal { background: rgba(237, 137, 54, 0.15); color: #fbd38d; }
  .badge-transferdebit { background: rgba(229, 62, 62, 0.15); color: #feb2b2; }
  .badge-transfercredit { background: rgba(41, 161, 156, 0.15); color: #81e6d9; }

  /* Modal */
  .modal-backdrop {
    position: fixed; inset: 0; background: rgba(0, 0, 0, 0.6);
    backdrop-filter: blur(4px);
    display: flex; justify-content: center; align-items: center; z-index: 100;
  }
  .modal {
    background: rgba(2, 44, 43, 0.95);
    backdrop-filter: blur(20px);
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 20px; padding: 2rem;
    width: 90%; max-width: 420px;
    box-shadow: 0 10px 40px rgba(0, 0, 0, 0.4);
  }
  .modal h3 { margin-bottom: 1.2rem; color: #fff; }
  .field { margin-bottom: 1rem; }
  .field label { display: block; font-size: 0.85rem; font-weight: 600; color: rgba(255, 255, 255, 0.7); margin-bottom: 0.3rem; }
  .field input, .field select {
    width: 100%; padding: 0.7rem 1rem;
    background: rgba(255, 255, 255, 0.05);
    border: 1px solid rgba(255, 255, 255, 0.1);
    border-radius: 12px; font-size: 0.9rem;
    color: #fff; font-family: inherit;
    transition: all 0.2s ease;
  }
  .field input:focus, .field select:focus {
    outline: none; border-color: #29a19c;
    box-shadow: 0 0 0 2px rgba(41, 161, 156, 0.2);
    background: rgba(255, 255, 255, 0.08);
  }
  .field input:disabled { background: rgba(255, 255, 255, 0.02); color: rgba(255, 255, 255, 0.35); }
  .field select option { background: #022c2b; color: #fff; }
  .modal-actions { display: flex; gap: 0.5rem; justify-content: flex-end; margin-top: 1.5rem; }

  @media (max-width: 768px) {
    .layout { grid-template-columns: 1fr; }
    .detail-header { flex-direction: column; gap: 1rem; }
    .balance-display { text-align: left; }
  }

  /* Debit Card Visual */
  .card-section { margin-bottom: 1.5rem; }
  .debit-card {
    background: linear-gradient(135deg, #006c68 0%, #022c2b 50%, #004440 100%);
    color: white;
    border-radius: 16px;
    padding: 1.5rem;
    max-width: 380px;
    position: relative;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
    font-family: 'Courier New', monospace;
    border: 1px solid rgba(255, 255, 255, 0.1);
  }
  .debit-card.frozen {
    background: linear-gradient(135deg, #2d3748 0%, #4a5568 50%, #718096 100%);
  }
  .debit-card.cancelled {
    background: linear-gradient(135deg, #742a2a 0%, #9b2c2c 50%, #c53030 100%);
    opacity: 0.6;
  }
  .card-chip {
    width: 40px; height: 28px;
    background: linear-gradient(135deg, #d4af37, #f0e68c);
    border-radius: 5px;
    margin-bottom: 1.2rem;
  }
  .card-number {
    font-size: 1.2rem;
    letter-spacing: 2px;
    margin-bottom: 1rem;
    font-weight: 600;
  }
  .card-bottom {
    display: flex;
    align-items: flex-end;
    gap: 1.5rem;
  }
  .card-label {
    display: block;
    font-size: 0.55rem;
    color: rgba(255,255,255,0.5);
    text-transform: uppercase;
    letter-spacing: 1px;
  }
  .card-value {
    display: block;
    font-size: 0.8rem;
    font-weight: 600;
    color: #fff;
  }
  .card-status-badge {
    margin-left: auto;
    padding: 0.2rem 0.5rem;
    border-radius: 10px;
    font-size: 0.6rem;
    font-weight: 700;
    text-transform: uppercase;
  }
  .status-active { background: rgba(163, 247, 191, 0.2); color: #a3f7bf; }
  .status-frozen { background: rgba(66, 153, 225, 0.3); color: #90cdf4; }
  .status-cancelled { background: rgba(229, 62, 62, 0.3); color: #feb2b2; }
  .card-limit {
    margin-top: 0.8rem;
    font-size: 0.65rem;
    color: rgba(255,255,255,0.4);
  }
  .card-actions {
    display: flex;
    gap: 0.5rem;
    margin-top: 0.8rem;
  }

  /* New card reveal */
  .new-card-reveal {
    background: rgba(253, 255, 132, 0.06);
    border: 1px solid rgba(253, 255, 132, 0.2);
    border-radius: 14px;
    padding: 1.5rem;
    margin-bottom: 1.5rem;
  }
  .new-card-reveal h4 { font-size: 1rem; color: #fdff84; margin-bottom: 0.3rem; }
  .card-warning { font-size: 0.8rem; color: #fbd38d; font-weight: 600; margin-bottom: 1rem; }
  .card-details-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.8rem;
  }
  .detail-label {
    display: block;
    font-size: 0.7rem;
    color: rgba(255, 255, 255, 0.45);
    text-transform: uppercase;
    font-weight: 600;
  }
  .detail-value {
    font-size: 1rem;
    color: #fff;
    font-weight: 700;
  }
  .mono { font-family: monospace; letter-spacing: 1px; }
  .cvv-highlight {
    background: rgba(229, 62, 62, 0.15);
    padding: 0.2rem 0.5rem;
    border-radius: 6px;
    color: #feb2b2;
    border: 1px solid rgba(229, 62, 62, 0.25);
  }
  .modal-subtitle {
    font-size: 0.85rem;
    color: rgba(255, 255, 255, 0.5);
    margin-bottom: 1rem;
  }
</style>
