<script>
  import { api } from '../lib/api.js';
  import { onMount } from 'svelte';

  export let user;

  let tab = 'stats'; // 'stats' | 'pending' | 'all' | 'transactions' | 'transfers'
  let stats = null;
  let pendingUsers = [];
  let allUsers = [];
  let adminTransactions = null;
  let adminTransfers = null;
  let loading = true;
  let error = '';
  let success = '';

  // Pagination
  let txPage = 1;
  let txPageSize = 20;
  let trPage = 1;
  let trPageSize = 20;

  // User detail modal
  let selectedUser = null;
  let detailLoading = false;
  let rejectionReason = '';
  let actionLoading = false;

  onMount(loadData);

  async function loadData() {
    loading = true;
    error = '';
    try {
      if (tab === 'stats') {
        stats = await api.getStats();
      } else if (tab === 'pending') {
        pendingUsers = await api.getPendingUsers();
      } else if (tab === 'all') {
        allUsers = await api.getAllUsers();
      } else if (tab === 'transactions') {
        adminTransactions = await api.getAdminTransactions({ page: txPage, pageSize: txPageSize });
      } else if (tab === 'transfers') {
        adminTransfers = await api.getAdminTransfers({ page: trPage, pageSize: trPageSize });
      }
    } catch (err) {
      error = err.message || 'Failed to load data';
    } finally {
      loading = false;
    }
  }

  async function switchTab(t) {
    tab = t;
    selectedUser = null;
    txPage = 1;
    trPage = 1;
    await loadData();
  }

  async function viewUser(userId) {
    detailLoading = true;
    error = '';
    try {
      selectedUser = await api.getUser(userId);
      rejectionReason = '';
    } catch (err) {
      error = err.message || 'Failed to load user details';
    } finally {
      detailLoading = false;
    }
  }

  async function handleVerify() {
    actionLoading = true;
    error = '';
    try {
      await api.updateKyc(selectedUser.id, { status: 'Verified' });
      success = `${selectedUser.fullName} has been verified. A checking account was auto-created.`;
      selectedUser = null;
      await loadData();
    } catch (err) {
      error = err.message || 'Verification failed';
    } finally {
      actionLoading = false;
    }
  }

  async function handleReject() {
    actionLoading = true;
    error = '';
    try {
      await api.updateKyc(selectedUser.id, {
        status: 'Rejected',
        rejectionReason: rejectionReason || 'ID verification failed.'
      });
      success = `${selectedUser.fullName} has been rejected.`;
      selectedUser = null;
      await loadData();
    } catch (err) {
      error = err.message || 'Rejection failed';
    } finally {
      actionLoading = false;
    }
  }

  async function goTxPage(newPage) {
    txPage = newPage;
    await loadData();
  }

  async function goTrPage(newPage) {
    trPage = newPage;
    await loadData();
  }

  function formatDate(d) {
    return new Date(d).toLocaleString();
  }

  function formatAmount(amount) {
    const val = parseFloat(amount);
    return `$${val.toFixed(2)}`;
  }

  function kycBadgeClass(status) {
    if (status === 'Verified') return 'status-verified';
    if (status === 'Rejected') return 'status-rejected';
    return 'status-pending';
  }
</script>

<div class="admin-panel">
  <h2>Admin Panel</h2>

  {#if error}
    <div class="alert alert-error">{error} <button on:click={() => error = ''}>x</button></div>
  {/if}
  {#if success}
    <div class="alert alert-success">{success} <button on:click={() => success = ''}>x</button></div>
  {/if}

  <!-- Tabs -->
  <div class="tabs">
    <button class="tab" class:active={tab === 'stats'} on:click={() => switchTab('stats')}>
      Dashboard
    </button>
    <button class="tab" class:active={tab === 'pending'} on:click={() => switchTab('pending')}>
      Pending KYC
      {#if pendingUsers.length > 0}
        <span class="badge-count">{pendingUsers.length}</span>
      {/if}
    </button>
    <button class="tab" class:active={tab === 'all'} on:click={() => switchTab('all')}>
      All Users
    </button>
    <button class="tab" class:active={tab === 'transactions'} on:click={() => switchTab('transactions')}>
      Transactions
    </button>
    <button class="tab" class:active={tab === 'transfers'} on:click={() => switchTab('transfers')}>
      Transfers
    </button>
  </div>

  {#if loading}
    <div class="loading">Loading...</div>

  {:else if tab === 'stats'}
    <!-- Stats Dashboard -->
    {#if stats}
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-label">Total Users</div>
          <div class="stat-value">{stats.totalUsers}</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Customers</div>
          <div class="stat-value">{stats.totalCustomers}</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Merchants</div>
          <div class="stat-value">{stats.totalMerchants}</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Employees</div>
          <div class="stat-value">{stats.totalEmployees}</div>
        </div>
      </div>
      <div class="stats-grid">
        <div class="stat-card stat-pending">
          <div class="stat-label">KYC Pending</div>
          <div class="stat-value">{stats.kycPending}</div>
        </div>
        <div class="stat-card stat-verified">
          <div class="stat-label">KYC Verified</div>
          <div class="stat-value">{stats.kycVerified}</div>
        </div>
        <div class="stat-card stat-rejected">
          <div class="stat-label">KYC Rejected</div>
          <div class="stat-value">{stats.kycRejected}</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Total Accounts</div>
          <div class="stat-value">{stats.totalAccounts}</div>
        </div>
      </div>
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-label">Active Accounts</div>
          <div class="stat-value">{stats.activeAccounts}</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Total Transactions</div>
          <div class="stat-value">{stats.totalTransactions}</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Total Transfers</div>
          <div class="stat-value">{stats.totalTransfers}</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Deposit Volume</div>
          <div class="stat-value">{formatAmount(stats.totalDepositVolume)}</div>
        </div>
      </div>
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-label">Withdrawal Volume</div>
          <div class="stat-value">{formatAmount(stats.totalWithdrawalVolume)}</div>
        </div>
        <div class="stat-card">
          <div class="stat-label">Transfer Volume</div>
          <div class="stat-value">{formatAmount(stats.totalTransferVolume)}</div>
        </div>
      </div>
    {/if}

  {:else if tab === 'pending'}
    <!-- Pending KYC Users -->
    {#if pendingUsers.length === 0}
      <div class="empty">No users pending KYC review.</div>
    {:else}
      <div class="user-list">
        {#each pendingUsers as u}
          <div class="user-card">
            <div class="user-info">
              <strong>{u.fullName}</strong>
              <span class="user-email">{u.email}</span>
              <span class="user-date">Registered: {formatDate(u.createdAt)}</span>
            </div>
            <button class="btn btn-review" on:click={() => viewUser(u.id)}>Review ID</button>
          </div>
        {/each}
      </div>
    {/if}

  {:else if tab === 'all'}
    <!-- All Users -->
    {#if allUsers.length === 0}
      <div class="empty">No users found.</div>
    {:else}
      <table class="users-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Role</th>
            <th>KYC Status</th>
            <th>National ID</th>
            <th>Registered</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {#each allUsers as u}
            <tr>
              <td>{u.fullName}</td>
              <td>{u.email}</td>
              <td><span class="role-badge role-{u.role.toLowerCase()}">{u.role}</span></td>
              <td>
                <span class={kycBadgeClass(u.kycStatus)}>{u.kycStatus}</span>
              </td>
              <td class="mono">{u.nationalIdNumber || '-'}</td>
              <td>{formatDate(u.createdAt)}</td>
              <td>
                <button class="btn-sm" on:click={() => viewUser(u.id)}>View</button>
              </td>
            </tr>
          {/each}
        </tbody>
      </table>
    {/if}

  {:else if tab === 'transactions'}
    <!-- Transaction Ledger (read-only) -->
    {#if adminTransactions && adminTransactions.items && adminTransactions.items.length > 0}
      <table class="users-table">
        <thead>
          <tr>
            <th>Date</th>
            <th>Account</th>
            <th>Type</th>
            <th>Amount</th>
            <th>Balance After</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          {#each adminTransactions.items as tx}
            <tr>
              <td>{formatDate(tx.createdAt)}</td>
              <td class="mono">{tx.accountNumber || tx.accountId}</td>
              <td><span class="badge badge-{tx.type.toLowerCase()}">{tx.type}</span></td>
              <td class={parseFloat(tx.amount) >= 0 ? 'amount-pos' : 'amount-neg'}>
                {formatAmount(tx.amount)}
              </td>
              <td>{formatAmount(tx.balanceAfter)}</td>
              <td class="desc">{tx.description}</td>
            </tr>
          {/each}
        </tbody>
      </table>
      <div class="pagination">
        <button class="btn-sm" disabled={txPage <= 1} on:click={() => goTxPage(txPage - 1)}>Prev</button>
        <span>Page {adminTransactions.page} of {adminTransactions.totalPages} ({adminTransactions.totalCount} total)</span>
        <button class="btn-sm" disabled={txPage >= adminTransactions.totalPages} on:click={() => goTxPage(txPage + 1)}>Next</button>
      </div>
    {:else}
      <div class="empty">No transactions found.</div>
    {/if}

  {:else if tab === 'transfers'}
    <!-- Transfers (read-only) -->
    {#if adminTransfers && adminTransfers.items && adminTransfers.items.length > 0}
      <table class="users-table">
        <thead>
          <tr>
            <th>Date</th>
            <th>From</th>
            <th>To</th>
            <th>Amount</th>
            <th>Status</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          {#each adminTransfers.items as tr}
            <tr>
              <td>{formatDate(tr.createdAt)}</td>
              <td class="mono">{tr.sourceAccountNumber}</td>
              <td class="mono">{tr.destinationAccountNumber}</td>
              <td>{formatAmount(tr.amount)}</td>
              <td><span class="badge badge-{tr.status.toLowerCase()}">{tr.status}</span></td>
              <td class="desc">{tr.description}</td>
            </tr>
          {/each}
        </tbody>
      </table>
      <div class="pagination">
        <button class="btn-sm" disabled={trPage <= 1} on:click={() => goTrPage(trPage - 1)}>Prev</button>
        <span>Page {adminTransfers.page} of {adminTransfers.totalPages} ({adminTransfers.totalCount} total)</span>
        <button class="btn-sm" disabled={trPage >= adminTransfers.totalPages} on:click={() => goTrPage(trPage + 1)}>Next</button>
      </div>
    {:else}
      <div class="empty">No transfers found.</div>
    {/if}
  {/if}

  <!-- User Detail / KYC Review Modal -->
  {#if selectedUser}
    <div class="modal-backdrop" on:click={() => selectedUser = null}>
      <div class="modal modal-wide" on:click|stopPropagation>
        {#if detailLoading}
          <div class="loading">Loading user details...</div>
        {:else}
          <h3>User Review: {selectedUser.fullName}</h3>
          <div class="detail-grid">
            <div class="detail-item">
              <label>Email</label>
              <span>{selectedUser.email}</span>
            </div>
            <div class="detail-item">
              <label>Role</label>
              <span class="role-badge role-{selectedUser.role.toLowerCase()}">{selectedUser.role}</span>
            </div>
            <div class="detail-item">
              <label>KYC Status</label>
              <span class={kycBadgeClass(selectedUser.kycStatus)}>
                {selectedUser.kycStatus}
                {#if selectedUser.kycStatus === 'Rejected' && selectedUser.rejectionReason}
                  - {selectedUser.rejectionReason}
                {/if}
              </span>
            </div>
            <div class="detail-item">
              <label>National ID</label>
              <span>{selectedUser.nationalIdNumber || 'Not provided'}</span>
            </div>
            <div class="detail-item">
              <label>Registered</label>
              <span>{formatDate(selectedUser.createdAt)}</span>
            </div>
          </div>

          <!-- ID Card Images -->
          <div class="id-images">
            <div class="id-image-box">
              <h4>ID Card - Front</h4>
              {#if selectedUser.idCardFrontImage}
                <img src={selectedUser.idCardFrontImage} alt="ID Front" />
              {:else}
                <div class="no-image">No front image</div>
              {/if}
            </div>
            <div class="id-image-box">
              <h4>ID Card - Back</h4>
              {#if selectedUser.idCardBackImage}
                <img src={selectedUser.idCardBackImage} alt="ID Back" />
              {:else}
                <div class="no-image">No back image</div>
              {/if}
            </div>
          </div>

          <!-- KYC Verify / Reject actions (only for Pending users) -->
          {#if selectedUser.kycStatus === 'Pending'}
            <div class="approval-actions">
              <div class="field">
                <label for="rejectionReason">Rejection Reason (required if rejecting)</label>
                <input id="rejectionReason" type="text" bind:value={rejectionReason} placeholder="e.g. ID image is blurry" />
              </div>
              <div class="modal-actions">
                <button class="btn btn-cancel" on:click={() => selectedUser = null}>Close</button>
                <button class="btn btn-reject" on:click={handleReject} disabled={actionLoading}>
                  {actionLoading ? 'Processing...' : 'Reject'}
                </button>
                <button class="btn btn-approve" on:click={handleVerify} disabled={actionLoading}>
                  {actionLoading ? 'Processing...' : 'Verify'}
                </button>
              </div>
            </div>
          {:else}
            <div class="modal-actions">
              <button class="btn btn-cancel" on:click={() => selectedUser = null}>Close</button>
            </div>
          {/if}
        {/if}
      </div>
    </div>
  {/if}
</div>

<style>
  .admin-panel { position: relative; }

  h2 { font-size: 1.5rem; margin-bottom: 1rem; color: #1a1a2e; }

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

  /* Tabs */
  .tabs {
    display: flex;
    gap: 0;
    margin-bottom: 1.5rem;
    border-bottom: 2px solid #e2e8f0;
    flex-wrap: wrap;
  }
  .tab {
    background: none;
    border: none;
    padding: 0.7rem 1.2rem;
    font-size: 0.85rem;
    font-weight: 600;
    color: #718096;
    cursor: pointer;
    border-bottom: 2px solid transparent;
    margin-bottom: -2px;
    transition: all 0.2s;
    display: flex;
    align-items: center;
    gap: 0.5rem;
  }
  .tab:hover { color: #2d3748; }
  .tab.active { color: #4299e1; border-bottom-color: #4299e1; }
  .badge-count {
    background: #e53e3e;
    color: white;
    font-size: 0.7rem;
    padding: 0.1rem 0.5rem;
    border-radius: 10px;
    font-weight: 700;
  }

  .loading { text-align: center; padding: 3rem; color: #718096; }
  .empty { text-align: center; padding: 3rem; color: #a0aec0; font-size: 1rem; }

  /* Stats Grid */
  .stats-grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 1rem;
    margin-bottom: 1rem;
  }
  .stat-card {
    background: white;
    border: 1px solid #e2e8f0;
    border-radius: 10px;
    padding: 1.2rem;
    text-align: center;
  }
  .stat-label {
    font-size: 0.75rem;
    font-weight: 600;
    color: #718096;
    text-transform: uppercase;
    margin-bottom: 0.4rem;
  }
  .stat-value {
    font-size: 1.5rem;
    font-weight: 700;
    color: #1a1a2e;
  }
  .stat-pending { border-left: 3px solid #d69e2e; }
  .stat-verified { border-left: 3px solid #48bb78; }
  .stat-rejected { border-left: 3px solid #e53e3e; }

  /* User List (pending) */
  .user-list { display: flex; flex-direction: column; gap: 0.5rem; }
  .user-card {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: white;
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    padding: 1rem 1.2rem;
    transition: border-color 0.2s;
  }
  .user-card:hover { border-color: #bee3f8; }
  .user-info { display: flex; flex-direction: column; gap: 0.2rem; }
  .user-info strong { font-size: 1rem; color: #2d3748; }
  .user-email { font-size: 0.85rem; color: #718096; }
  .user-date { font-size: 0.75rem; color: #a0aec0; }

  /* Users Table */
  .users-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.85rem;
    background: white;
    border-radius: 8px;
    overflow: hidden;
    box-shadow: 0 1px 4px rgba(0,0,0,0.06);
  }
  .users-table th {
    text-align: left;
    padding: 0.7rem 0.8rem;
    border-bottom: 2px solid #e2e8f0;
    color: #718096;
    font-size: 0.75rem;
    text-transform: uppercase;
    background: #f7fafc;
  }
  .users-table td {
    padding: 0.7rem 0.8rem;
    border-bottom: 1px solid #f0f2f5;
  }
  .users-table tr:hover { background: #f7fafc; }
  .mono { font-family: monospace; font-size: 0.8rem; }

  /* Badges */
  .role-badge {
    display: inline-block;
    padding: 0.15rem 0.5rem;
    border-radius: 12px;
    font-size: 0.7rem;
    font-weight: 600;
    text-transform: uppercase;
  }
  .role-admin { background: #e9d8fd; color: #6b46c1; }
  .role-merchant { background: #bee3f8; color: #2b6cb0; }
  .role-customer { background: #c6f6d5; color: #22543d; }
  .role-employee { background: #feebc8; color: #c05621; }

  .status-verified { color: #22543d; font-weight: 600; }
  .status-rejected { color: #c53030; font-weight: 600; }
  .status-pending { color: #d69e2e; font-weight: 600; }

  .badge {
    display: inline-block; padding: 0.15rem 0.5rem; border-radius: 12px;
    font-size: 0.7rem; font-weight: 600; text-transform: uppercase;
  }
  .badge-deposit { background: #c6f6d5; color: #22543d; }
  .badge-withdrawal { background: #feebc8; color: #c05621; }
  .badge-transferdebit { background: #fed7d7; color: #c53030; }
  .badge-transfercredit { background: #bee3f8; color: #2b6cb0; }
  .badge-completed { background: #c6f6d5; color: #22543d; }
  .badge-pending { background: #fefcbf; color: #975a16; }
  .badge-failed { background: #fed7d7; color: #c53030; }

  .amount-pos { color: #22543d; font-weight: 600; }
  .amount-neg { color: #c53030; font-weight: 600; }
  .desc { color: #718096; max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

  /* Pagination */
  .pagination {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 1rem;
    padding: 1rem;
    color: #718096;
    font-size: 0.85rem;
  }

  /* Buttons */
  .btn {
    padding: 0.5rem 1rem;
    border: none;
    border-radius: 6px;
    font-size: 0.85rem;
    font-weight: 600;
    cursor: pointer;
    transition: opacity 0.2s;
  }
  .btn:hover { opacity: 0.85; }
  .btn:disabled { opacity: 0.5; cursor: not-allowed; }
  .btn-review { background: #4299e1; color: white; }
  .btn-approve { background: #48bb78; color: white; }
  .btn-reject { background: #e53e3e; color: white; }
  .btn-cancel { background: #e2e8f0; color: #4a5568; }
  .btn-sm {
    background: #edf2f7;
    color: #4a5568;
    border: none;
    padding: 0.3rem 0.6rem;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.8rem;
    font-weight: 600;
  }
  .btn-sm:hover { background: #e2e8f0; }
  .btn-sm:disabled { opacity: 0.5; cursor: not-allowed; }

  /* Modal */
  .modal-backdrop {
    position: fixed;
    inset: 0;
    background: rgba(0,0,0,0.4);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 100;
  }
  .modal {
    background: white;
    border-radius: 12px;
    padding: 2rem;
    width: 90%;
    max-width: 420px;
    box-shadow: 0 10px 40px rgba(0,0,0,0.15);
    max-height: 90vh;
    overflow-y: auto;
  }
  .modal-wide {
    max-width: 700px;
  }
  .modal h3 { margin-bottom: 1.2rem; color: #1a1a2e; }
  .modal-actions { display: flex; gap: 0.5rem; justify-content: flex-end; margin-top: 1.5rem; }

  /* Detail Grid */
  .detail-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.8rem;
    margin-bottom: 1.5rem;
  }
  .detail-item label {
    display: block;
    font-size: 0.75rem;
    font-weight: 600;
    color: #718096;
    text-transform: uppercase;
    margin-bottom: 0.2rem;
  }
  .detail-item span { font-size: 0.9rem; color: #2d3748; }

  /* ID Images */
  .id-images {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1rem;
    margin-bottom: 1.5rem;
  }
  .id-image-box {
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    overflow: hidden;
  }
  .id-image-box h4 {
    background: #f7fafc;
    padding: 0.5rem 0.8rem;
    font-size: 0.8rem;
    color: #4a5568;
    border-bottom: 1px solid #e2e8f0;
  }
  .id-image-box img {
    width: 100%;
    display: block;
    cursor: pointer;
  }
  .no-image {
    padding: 2rem;
    text-align: center;
    color: #a0aec0;
    font-size: 0.85rem;
  }

  /* Approval Actions */
  .approval-actions { border-top: 1px solid #e2e8f0; padding-top: 1rem; }
  .field { margin-bottom: 1rem; }
  .field label {
    display: block;
    font-size: 0.85rem;
    font-weight: 600;
    color: #4a5568;
    margin-bottom: 0.3rem;
  }
  .field input {
    width: 100%;
    padding: 0.6rem 0.8rem;
    border: 1px solid #e2e8f0;
    border-radius: 6px;
    font-size: 0.9rem;
  }
  .field input:focus { outline: none; border-color: #4299e1; }

  @media (max-width: 768px) {
    .stats-grid { grid-template-columns: repeat(2, 1fr); }
    .detail-grid { grid-template-columns: 1fr; }
    .id-images { grid-template-columns: 1fr; }
    .users-table { font-size: 0.8rem; }
  }
</style>
