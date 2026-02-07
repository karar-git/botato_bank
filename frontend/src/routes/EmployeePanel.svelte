<script>
  import { api } from '../lib/api.js';

  export let user;

  let file = null;
  let fileName = '';
  let uploading = false;
  let error = '';
  let result = null;

  function handleFileChange(e) {
    const f = e.target.files[0];
    if (f) {
      file = f;
      fileName = f.name;
    }
  }

  async function handleUpload() {
    if (!file) {
      error = 'Please select a CSV file to upload.';
      return;
    }
    uploading = true;
    error = '';
    result = null;
    try {
      result = await api.uploadCsv(file);
    } catch (err) {
      error = err.message || 'CSV upload failed';
    } finally {
      uploading = false;
    }
  }

  function reset() {
    file = null;
    fileName = '';
    result = null;
    error = '';
  }
</script>

<div class="employee-panel">
  <h2>Employee Panel</h2>
  <p class="subtitle">Upload CSV instruction files to process bulk deposits and withdrawals.</p>

  {#if error}
    <div class="alert alert-error">{error} <button on:click={() => error = ''}>x</button></div>
  {/if}

  <div class="upload-section">
    <div class="csv-format">
      <h3>CSV Format</h3>
      <p>Your CSV file must have a header row with these columns:</p>
      <code>NationalId,Amount,Operation</code>
      <p class="hint">
        <strong>NationalId</strong> — The user's national ID number<br/>
        <strong>Amount</strong> — The amount (positive number)<br/>
        <strong>Operation</strong> — Either <code>DEPOSIT</code> or <code>WITHDRAW</code>
      </p>
      <div class="example">
        <strong>Example:</strong>
        <pre>NationalId,Amount,Operation
AB123456,500.00,DEPOSIT
CD789012,250.00,WITHDRAW
EF345678,1000.00,DEPOSIT</pre>
      </div>
    </div>

    <div class="upload-card">
      <div class="file-upload" class:has-file={fileName}>
        <input type="file" accept=".csv,text/csv" on:change={handleFileChange} />
        <div class="file-upload-label">
          {#if fileName}
            <span class="file-name">{fileName}</span>
          {:else}
            <span class="file-placeholder">Click or drag to select a CSV file</span>
          {/if}
        </div>
      </div>
      <div class="upload-actions">
        <button class="btn btn-upload" on:click={handleUpload} disabled={uploading || !file}>
          {uploading ? 'Processing...' : 'Upload & Process'}
        </button>
        {#if result}
          <button class="btn btn-reset" on:click={reset}>Upload Another</button>
        {/if}
      </div>
    </div>
  </div>

  <!-- Results -->
  {#if result}
    <div class="results-section">
      <h3>Processing Results</h3>
      <div class="results-summary">
        <div class="summary-item">
          <span class="summary-label">Total Rows</span>
          <span class="summary-value">{result.totalRows}</span>
        </div>
        <div class="summary-item summary-success">
          <span class="summary-label">Successful</span>
          <span class="summary-value">{result.successCount}</span>
        </div>
        <div class="summary-item summary-failure">
          <span class="summary-label">Failed</span>
          <span class="summary-value">{result.failureCount}</span>
        </div>
        <div class="summary-item">
          <span class="summary-label">Processed At</span>
          <span class="summary-value">{new Date(result.processedAt).toLocaleString()}</span>
        </div>
      </div>

      {#if result.results && result.results.length > 0}
        <table class="results-table">
          <thead>
            <tr>
              <th>Row</th>
              <th>National ID</th>
              <th>Amount</th>
              <th>Operation</th>
              <th>Status</th>
              <th>Account</th>
              <th>Balance After</th>
              <th>Error</th>
            </tr>
          </thead>
          <tbody>
            {#each result.results as row}
              <tr class={row.success ? '' : 'row-error'}>
                <td>{row.rowNumber}</td>
                <td class="mono">{row.nationalId}</td>
                <td>${parseFloat(row.amount).toFixed(2)}</td>
                <td><span class="badge badge-{row.operation.toLowerCase()}">{row.operation}</span></td>
                <td>
                  {#if row.success}
                    <span class="status-ok">OK</span>
                  {:else}
                    <span class="status-fail">FAILED</span>
                  {/if}
                </td>
                <td class="mono">{row.accountNumber || '-'}</td>
                <td>{row.balanceAfter != null ? `$${parseFloat(row.balanceAfter).toFixed(2)}` : '-'}</td>
                <td class="error-cell">{row.error || ''}</td>
              </tr>
            {/each}
          </tbody>
        </table>
      {/if}
    </div>
  {/if}
</div>

<style>
  .employee-panel { position: relative; }
  h2 { font-size: 1.5rem; margin-bottom: 0.3rem; color: var(--brand-dark, #34495E); }
  .subtitle { color: var(--text-muted, #7f8c8d); font-size: 0.9rem; margin-bottom: 1.5rem; }

  /* Alerts */
  .alert {
    padding: 0.8rem 1rem; border-radius: 12px; margin-bottom: 1rem;
    display: flex; justify-content: space-between; align-items: center; font-size: 0.9rem;
  }
  .alert button { background: none; border: none; cursor: pointer; font-size: 1rem; opacity: 0.6; color: inherit; }
  .alert-error { background: #fef2f2; color: #dc2626; border: 1px solid #fecaca; }

  /* Upload Section */
  .upload-section {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 1.5rem;
    margin-bottom: 2rem;
  }
  .csv-format {
    background: white;
    border: 1px solid #e2e8f0;
    border-radius: 16px;
    padding: 1.5rem;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.04);
  }
  .csv-format h3 { font-size: 1rem; margin-bottom: 0.5rem; color: var(--brand-dark, #34495E); }
  .csv-format p { font-size: 0.85rem; color: var(--text-muted, #7f8c8d); margin-bottom: 0.5rem; }
  .csv-format code {
    display: inline-block;
    background: rgba(52, 152, 219, 0.08);
    padding: 0.3rem 0.6rem;
    border-radius: 6px;
    font-size: 0.85rem;
    color: var(--brand-primary, #3498DB);
    margin-bottom: 0.5rem;
    border: 1px solid rgba(52, 152, 219, 0.15);
  }
  .hint { font-size: 0.8rem; line-height: 1.6; color: var(--text-muted, #7f8c8d); }
  .hint strong { color: var(--text-main, #2c3e50); }
  .hint code {
    background: #f8fafc;
    padding: 0.1rem 0.3rem;
    border-radius: 3px;
    font-size: 0.8rem;
    color: #d97706;
    border: none;
  }
  .example {
    margin-top: 0.8rem;
    background: #f8fafc;
    border: 1px solid #e2e8f0;
    border-radius: 10px;
    padding: 0.8rem;
  }
  .example strong { font-size: 0.8rem; color: var(--text-muted, #7f8c8d); }
  .example pre {
    margin-top: 0.3rem;
    font-size: 0.8rem;
    color: var(--text-main, #2c3e50);
    white-space: pre;
    overflow-x: auto;
  }

  .upload-card {
    background: white;
    border: 1px solid #e2e8f0;
    border-radius: 16px;
    padding: 1.5rem;
    display: flex;
    flex-direction: column;
    justify-content: center;
    gap: 1rem;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.04);
  }
  .file-upload {
    position: relative;
    border: 2px dashed #d1d5db;
    border-radius: 12px;
    padding: 2rem;
    text-align: center;
    cursor: pointer;
    transition: all 0.2s ease;
  }
  .file-upload:hover { border-color: var(--brand-primary, #3498DB); background: #f0f7ff; }
  .file-upload.has-file { border-color: #27ae60; background: rgba(39, 174, 96, 0.04); }
  .file-upload input[type="file"] {
    position: absolute; inset: 0; opacity: 0; cursor: pointer; width: 100%; height: 100%;
  }
  .file-placeholder { color: var(--text-muted, #7f8c8d); font-size: 0.9rem; }
  .file-name { color: #27ae60; font-weight: 600; font-size: 0.9rem; }
  .upload-actions { display: flex; gap: 0.5rem; }

  .btn {
    padding: 0.6rem 1.2rem; border: none; border-radius: 20px;
    font-size: 0.9rem; font-weight: 600; cursor: pointer; transition: all 0.2s ease;
    font-family: inherit;
  }
  .btn:hover { transform: scale(1.03); }
  .btn:disabled { opacity: 0.5; cursor: not-allowed; transform: none; }
  .btn-upload {
    background: var(--brand-dark, #34495E);
    color: white; flex: 1;
  }
  .btn-upload:hover { background: var(--brand-primary, #3498DB); }
  .btn-reset { background: #f8fafc; color: var(--text-muted, #7f8c8d); border: 1px solid #e2e8f0; }

  /* Results */
  .results-section {
    background: white;
    border: 1px solid #e2e8f0;
    border-radius: 16px;
    padding: 1.5rem;
    box-shadow: 0 2px 10px rgba(0, 0, 0, 0.04);
  }
  .results-section h3 { font-size: 1.1rem; margin-bottom: 1rem; color: var(--brand-dark, #34495E); }

  .results-summary {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: 1rem;
    margin-bottom: 1.5rem;
  }
  .summary-item {
    text-align: center;
    padding: 0.8rem;
    background: #f8fafc;
    border: 1px solid #e2e8f0;
    border-radius: 12px;
  }
  .summary-label { display: block; font-size: 0.7rem; font-weight: 600; color: var(--text-muted, #7f8c8d); text-transform: uppercase; }
  .summary-value { display: block; font-size: 1.3rem; font-weight: 700; color: var(--brand-dark, #34495E); margin-top: 0.2rem; }
  .summary-success { border-left: 3px solid #27ae60; }
  .summary-failure { border-left: 3px solid #dc2626; }

  .results-table {
    width: 100%; border-collapse: collapse; font-size: 0.8rem; color: var(--text-main, #2c3e50);
  }
  .results-table th {
    text-align: left; padding: 0.6rem 0.6rem; border-bottom: 2px solid #e2e8f0;
    color: var(--text-muted, #7f8c8d); font-size: 0.7rem; text-transform: uppercase;
    background: #f8fafc;
  }
  .results-table td {
    padding: 0.5rem 0.6rem; border-bottom: 1px solid #f1f5f9;
  }
  .results-table tr:hover { background: #f8fafc; }
  .row-error { background: #fef2f2; }
  .row-error:hover { background: #fee2e2; }
  .mono { font-family: monospace; font-size: 0.8rem; }

  .badge {
    display: inline-block; padding: 0.1rem 0.4rem; border-radius: 10px;
    font-size: 0.65rem; font-weight: 600; text-transform: uppercase;
  }
  .badge-deposit { background: #f0fdf4; color: #16a34a; }
  .badge-withdraw { background: #fff7ed; color: #ea580c; }

  .status-ok { color: #16a34a; font-weight: 700; }
  .status-fail { color: #dc2626; font-weight: 700; }
  .error-cell { color: #dc2626; font-size: 0.75rem; max-width: 200px; }

  @media (max-width: 768px) {
    .upload-section { grid-template-columns: 1fr; }
    .results-summary { grid-template-columns: repeat(2, 1fr); }
    .results-table { font-size: 0.75rem; }
  }
</style>
