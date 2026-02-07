# CoreBank - Production-Grade Digital Banking System

## Quick Start

```bash
# Backend (requires .NET 8 SDK)
cd CoreBank/src
dotnet restore
dotnet run

# Frontend (requires Node.js)
cd frontend
npm install
npm run dev
```

Backend runs at `http://localhost:5000` (Swagger at `/swagger`)
Frontend runs at `http://localhost:5173` (proxies API calls to backend)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        SVELTE FRONTEND                         │
│              (SPA - Login, Dashboard, Transfers)                │
└─────────────────────────┬───────────────────────────────────────┘
                          │ HTTP/JSON + JWT Bearer Token
┌─────────────────────────▼───────────────────────────────────────┐
│                     ASP.NET CORE API                            │
│  ┌────────────┐ ┌──────────────┐ ┌────────────┐ ┌───────────┐  │
│  │ AuthCtrl   │ │ AccountsCtrl │ │ WalletCtrl │ │TransferCtrl│ │
│  └─────┬──────┘ └──────┬───────┘ └─────┬──────┘ └─────┬─────┘  │
│        │               │               │               │        │
│  ┌─────▼───────────────▼───────────────▼───────────────▼─────┐  │
│  │              EXCEPTION HANDLING MIDDLEWARE                 │  │
│  │    Maps DomainExceptions → structured HTTP error responses│  │
│  └───────────────────────┬───────────────────────────────────┘  │
│                          │                                      │
│  ┌───────────────────────▼───────────────────────────────────┐  │
│  │                    SERVICE LAYER                          │  │
│  │  ┌──────────┐  ┌─────────────┐  ┌──────────────────────┐ │  │
│  │  │AuthService│  │AccountService│  │  TransactionService │ │  │
│  │  └──────────┘  └─────────────┘  └──────────────────────┘ │  │
│  │                                                           │  │
│  │  ┌──────────────────────────────────────────────────────┐ │  │
│  │  │              CORE BANKING ENGINE                     │ │  │
│  │  │  - Deposit / Withdraw / Transfer                    │ │  │
│  │  │  - Atomic ledger writes + cached balance updates    │ │  │
│  │  │  - Optimistic concurrency control (RowVersion)      │ │  │
│  │  │  - Idempotency key deduplication                    │ │  │
│  │  │  - Retry with exponential backoff on OCC conflict   │ │  │
│  │  └──────────────────────────────────────────────────────┘ │  │
│  └───────────────────────┬───────────────────────────────────┘  │
│                          │                                      │
│  ┌───────────────────────▼───────────────────────────────────┐  │
│  │                  EF CORE (DbContext)                      │  │
│  │  - Concurrency tokens on Account.RowVersion               │  │
│  │  - Unique constraints on AccountNumber, Email             │  │
│  │  - Unique index on IdempotencyKey                         │  │
│  └───────────────────────┬───────────────────────────────────┘  │
└──────────────────────────┼──────────────────────────────────────┘
                           │
              ┌────────────▼────────────┐
              │   SQLite / PostgreSQL   │
              │                         │
              │  Users                  │
              │  Accounts               │
              │  LedgerEntries          │
              │  Transfers              │
              │  IdempotencyRecords     │
              └─────────────────────────┘
```

---

## Database Schema

### Users
| Column       | Type         | Constraints           |
|-------------|--------------|-----------------------|
| Id          | GUID (PK)    | NOT NULL              |
| FullName    | VARCHAR(100) | NOT NULL              |
| Email       | VARCHAR(255) | NOT NULL, UNIQUE      |
| PasswordHash| TEXT         | NOT NULL (bcrypt)     |
| CreatedAt   | DATETIME     | NOT NULL              |
| UpdatedAt   | DATETIME     | NULLABLE              |

### Accounts
| Column        | Type          | Constraints                      |
|--------------|---------------|----------------------------------|
| Id           | GUID (PK)     | NOT NULL                         |
| AccountNumber| VARCHAR(30)   | NOT NULL, UNIQUE                 |
| UserId       | GUID (FK)     | NOT NULL → Users.Id              |
| Type         | INT (enum)    | Checking=1, Savings=2            |
| Status       | INT (enum)    | Active=1, Frozen=2, Closed=3     |
| CachedBalance| DECIMAL(18,2) | NOT NULL, DEFAULT 0              |
| Currency     | VARCHAR(3)    | NOT NULL, DEFAULT 'USD'          |
| RowVersion   | BIGINT        | CONCURRENCY TOKEN                |
| CreatedAt    | DATETIME      | NOT NULL                         |
| UpdatedAt    | DATETIME      | NULLABLE                         |

### LedgerEntries (APPEND-ONLY - source of truth)
| Column       | Type          | Constraints                       |
|-------------|---------------|-----------------------------------|
| Id          | GUID (PK)     | NOT NULL                          |
| AccountId   | GUID (FK)     | NOT NULL → Accounts.Id            |
| Amount      | DECIMAL(18,2) | NOT NULL (+ = credit, - = debit)  |
| Type        | INT (enum)    | Deposit/Withdrawal/TransferDebit/Credit |
| Status      | INT (enum)    | Completed/Failed/Reversed         |
| BalanceAfter| DECIMAL(18,2) | Running balance after this entry   |
| TransferId  | GUID (FK)     | NULLABLE → Transfers.Id           |
| Description | VARCHAR(500)  |                                   |
| CreatedAt   | DATETIME      | NOT NULL, IMMUTABLE               |

### Transfers
| Column              | Type          | Constraints                    |
|--------------------|---------------|--------------------------------|
| Id                 | GUID (PK)     | NOT NULL                       |
| SourceAccountId    | GUID (FK)     | NOT NULL → Accounts.Id         |
| DestinationAccountId| GUID (FK)    | NOT NULL → Accounts.Id         |
| Amount             | DECIMAL(18,2) | NOT NULL                       |
| Currency           | VARCHAR(3)    | NOT NULL                       |
| Status             | INT (enum)    | Pending/Completed/Failed       |
| Description        | VARCHAR(500)  |                                |
| IdempotencyKey     | VARCHAR(100)  | UNIQUE                         |
| FailureReason      | VARCHAR(500)  | NULLABLE                       |
| CreatedAt          | DATETIME      | NOT NULL                       |
| CompletedAt        | DATETIME      | NULLABLE                       |

### IdempotencyRecords
| Column           | Type         | Constraints                    |
|-----------------|--------------|--------------------------------|
| Id              | GUID (PK)    | NOT NULL                       |
| Key             | VARCHAR(100) | NOT NULL                       |
| UserId          | GUID         | NOT NULL                       |
| OperationPath   | VARCHAR(200) | NOT NULL                       |
| RequestBody     | TEXT         | NULLABLE                       |
| ResponseStatusCode | INT       | NULLABLE                       |
| ResponseBody    | TEXT         | NULLABLE                       |
| IsCompleted     | BOOLEAN      | NOT NULL                       |
| CreatedAt       | DATETIME     | NOT NULL                       |
| CompletedAt     | DATETIME     | NULLABLE                       |

**Unique Index:** (Key, UserId)

---

## Transfer Execution - Step by Step

```
CLIENT                          BANKING ENGINE                       DATABASE
  │                                  │                                  │
  │  POST /api/transfers             │                                  │
  │  { source, dest, amount,         │                                  │
  │    idempotencyKey }              │                                  │
  │ ─────────────────────────────────>                                  │
  │                                  │                                  │
  │                           1. Validate amount                        │
  │                              - > 0, <= 1B                           │
  │                              - max 2 decimal places                 │
  │                              - not NaN/Infinity                     │
  │                                  │                                  │
  │                           2. Check idempotency key                  │
  │                              SELECT FROM IdempotencyRecords ────────>
  │                              WHERE Key=@key AND UserId=@uid         │
  │                              <─── NULL (new) or cached result ──────│
  │                                  │                                  │
  │                           3. BEGIN TRANSACTION ─────────────────────>
  │                                  │                                  │
  │                           4. Load source account                    │
  │                              SELECT * FROM Accounts ────────────────>
  │                              WHERE AccountNumber=@src               │
  │                              (reads RowVersion=N)                   │
  │                                  │                                  │
  │                           5. Load destination account               │
  │                              SELECT * FROM Accounts ────────────────>
  │                              WHERE AccountNumber=@dest              │
  │                              (reads RowVersion=M)                   │
  │                                  │                                  │
  │                           6. Validate:                              │
  │                              - src != dest (no self-transfer)       │
  │                              - caller owns source account           │
  │                              - both accounts Active                 │
  │                              - source.CachedBalance >= amount       │
  │                                  │                                  │
  │                           7. Create Transfer record ────────────────>
  │                              INSERT INTO Transfers                  │
  │                                  │                                  │
  │                           8. Create debit ledger entry ─────────────>
  │                              INSERT INTO LedgerEntries              │
  │                              (AccountId=src, Amount=-N)             │
  │                                  │                                  │
  │                           9. Create credit ledger entry ────────────>
  │                              INSERT INTO LedgerEntries              │
  │                              (AccountId=dest, Amount=+N)            │
  │                                  │                                  │
  │                          10. Update source account ─────────────────>
  │                              UPDATE Accounts SET                    │
  │                              CachedBalance=X, RowVersion=N+1       │
  │                              WHERE Id=@src AND RowVersion=N         │
  │                              ┌──── 0 rows? → RETRY (OCC conflict)  │
  │                              │                                      │
  │                          11. Update dest account ───────────────────>
  │                              UPDATE Accounts SET                    │
  │                              CachedBalance=Y, RowVersion=M+1       │
  │                              WHERE Id=@dest AND RowVersion=M        │
  │                                  │                                  │
  │                          12. COMMIT TRANSACTION ────────────────────>
  │                                  │                                  │
  │                          13. Store idempotency result ──────────────>
  │                              INSERT INTO IdempotencyRecords         │
  │                                  │                                  │
  │  <────── 200 OK ────────────────│                                  │
  │  { transferId, status,           │                                  │
  │    amount, completedAt }         │                                  │
```

---

## Concurrency Strategy

### Approach: Optimistic Concurrency Control (OCC)

**How it works:**

Every `Account` row has a `RowVersion` column configured as an EF Core concurrency token. When EF Core generates an `UPDATE` statement, it includes `WHERE RowVersion = @originalValue`. If another transaction modified the row between our read and write, the `WHERE` clause matches 0 rows, and EF Core throws `DbUpdateConcurrencyException`.

**What happens when two transfers hit the same account at the same millisecond:**

```
T1: SELECT Account A → RowVersion = 5, Balance = $1000
T2: SELECT Account A → RowVersion = 5, Balance = $1000

T1: UPDATE Account A SET Balance=$800, RowVersion=6 WHERE RowVersion=5
    → 1 row affected → SUCCESS

T2: UPDATE Account A SET Balance=$700, RowVersion=6 WHERE RowVersion=5
    → 0 rows affected → DbUpdateConcurrencyException!

T2: RETRY → Re-reads Account A → RowVersion = 6, Balance = $800
T2: UPDATE Account A SET Balance=$500, RowVersion=7 WHERE RowVersion=6
    → 1 row affected → SUCCESS
```

**Result:** Both transfers execute correctly. No money is lost. No double-spend.

**Why OCC over pessimistic locking:**

| Factor | OCC | Pessimistic (SELECT FOR UPDATE) |
|--------|-----|------|
| Lock duration | Zero (no locks held) | Entire transaction |
| Deadlock risk | None | High (when locking multiple accounts) |
| Throughput | Higher under low contention | Higher under extreme contention |
| Complexity | Retry logic needed | Lock ordering needed |
| Our choice | **Yes** - banking typically has low per-account contention | |

**Livelock prevention:** Max 3 retries with exponential backoff (50ms, 100ms, 200ms).

---

## API Structure

```
POST   /api/auth/register          → Register + auto-create checking account
POST   /api/auth/login             → JWT token
GET    /api/auth/me                → Current user info

GET    /api/accounts               → List user's accounts
POST   /api/accounts               → Create new account
GET    /api/accounts/{id}          → Get account details
GET    /api/accounts/{id}/reconcile → Verify balance integrity

POST   /api/accounts/{id}/deposit  → Deposit funds
POST   /api/accounts/{id}/withdraw → Withdraw funds

POST   /api/transfers              → Atomic fund transfer

GET    /api/accounts/{id}/transactions          → Paginated history
GET    /api/accounts/{id}/transactions/export/csv  → CSV export
GET    /api/accounts/{id}/transactions/export/xlsx → XLSX export
```

---

## Design Decisions - Core Banking Justification

### 1. Ledger-based balance (not mutable field)
**Banking term:** General Ledger / Journal Entry system
- `CachedBalance` is a **materialized aggregate**, not the source of truth
- True balance = `SUM(LedgerEntries.Amount) WHERE AccountId = X`
- The `/reconcile` endpoint verifies these match (auditor requirement)

### 2. No direct balance mutation
- No API endpoint, admin panel, or database trigger can SET a balance
- Every cent change must flow through `BankingEngine.Deposit/Withdraw/Transfer`
- This is the **maker-checker principle** in banking

### 3. Immutable ledger entries
- `LedgerEntries` are append-only; no UPDATE or DELETE
- Reversals create new counter-entries (debit reversal = new credit)
- This is standard **double-entry bookkeeping**

### 4. Double-entry for transfers
- Every transfer creates exactly 2 entries: debit on source, credit on destination
- Linked by `TransferId` for full audit trail
- **Conservation of money:** `SUM(all ledger entries) = total deposits - total withdrawals`

### 5. Idempotency
- Transfers require a client-provided `IdempotencyKey`
- Duplicate key → return cached result, no re-execution
- Critical for surviving network failures + client retries

### 6. Input validation
- Negative amounts: rejected
- Zero amounts: rejected
- Sub-cent precision (e.g., $1.999): rejected
- Overflow values (> $1B): rejected
- Self-transfers: rejected
- Unauthorized account access: rejected at service layer

---

## Running Tests

```bash
cd CoreBank
dotnet test
```

Tests cover:
- Deposit creates correct ledger entry and updates balance
- Withdrawal fails on insufficient funds
- Transfer atomicity (debit + credit in same transaction)
- Idempotency (same key returns same result, no double-execution)
- Self-transfer rejection
- Negative/zero/sub-cent amount rejection
- Unauthorized account access
- Balance reconciliation (cached == ledger-derived)
- wow
