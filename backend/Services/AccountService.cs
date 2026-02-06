using Microsoft.EntityFrameworkCore;
using CoreBank.Data;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.Domain.Exceptions;
using CoreBank.DTOs.Requests;
using CoreBank.DTOs.Responses;

namespace CoreBank.Services;

public interface IAccountService
{
    Task<AccountResponse> CreateAccountAsync(Guid userId, CreateAccountRequest request);
    Task<List<AccountResponse>> GetUserAccountsAsync(Guid userId);
    Task<AccountResponse> GetAccountAsync(Guid userId, Guid accountId);
    Task<decimal> GetLedgerBalanceAsync(Guid accountId);
    Task<BalanceReconciliationResponse> ReconcileBalanceAsync(Guid userId, Guid accountId);
    Task ValidateAccountOwnership(Guid userId, Guid accountId);
}

public class AccountService : IAccountService
{
    private readonly BankDbContext _db;
    private readonly ILogger<AccountService> _logger;

    public AccountService(BankDbContext db, ILogger<AccountService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AccountResponse> CreateAccountAsync(Guid userId, CreateAccountRequest request)
    {
        if (!Enum.TryParse<AccountType>(request.Type, true, out var accountType))
            throw new InvalidAmountException($"Invalid account type: {request.Type}. Valid types: Checking, Savings.");

        var account = new Account
        {
            Id = Guid.NewGuid(),
            AccountNumber = GenerateAccountNumber(accountType == AccountType.Checking ? "CHK" : "SAV"),
            UserId = userId,
            Type = accountType,
            Status = AccountStatus.Active,
            CachedBalance = 0m,
            Currency = "USD",
            CreatedAt = DateTime.UtcNow,
            RowVersion = 0
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Account {AccountNumber} ({Type}) created for user {UserId}",
            account.AccountNumber, account.Type, userId);

        return MapAccountResponse(account);
    }

    public async Task<List<AccountResponse>> GetUserAccountsAsync(Guid userId)
    {
        var accounts = await _db.Accounts
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        return accounts.Select(MapAccountResponse).ToList();
    }

    public async Task<AccountResponse> GetAccountAsync(Guid userId, Guid accountId)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        if (account == null)
            throw new AccountNotFoundException(accountId);

        return MapAccountResponse(account);
    }

    /// <summary>
    /// Derives the TRUE balance by summing all ledger entries.
    /// This is the source of truth - CachedBalance is just a performance optimization.
    /// </summary>
    public async Task<decimal> GetLedgerBalanceAsync(Guid accountId)
    {
        return await _db.LedgerEntries
            .Where(l => l.AccountId == accountId && l.Status == TransactionStatus.Completed)
            .SumAsync(l => l.Amount);
    }

    /// <summary>
    /// Reconciliation: compares CachedBalance against ledger-derived balance.
    /// In a real bank, any discrepancy triggers an alert to the operations team.
    /// </summary>
    public async Task<BalanceReconciliationResponse> ReconcileBalanceAsync(Guid userId, Guid accountId)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

        if (account == null)
            throw new AccountNotFoundException(accountId);

        var ledgerBalance = await GetLedgerBalanceAsync(accountId);
        var entryCount = await _db.LedgerEntries
            .CountAsync(l => l.AccountId == accountId && l.Status == TransactionStatus.Completed);

        var isReconciled = account.CachedBalance == ledgerBalance;

        if (!isReconciled)
        {
            _logger.LogCritical(
                "BALANCE MISMATCH for account {AccountId}: Cached={Cached}, Ledger={Ledger}",
                accountId, account.CachedBalance, ledgerBalance);
        }

        return new BalanceReconciliationResponse
        {
            AccountId = account.Id,
            AccountNumber = account.AccountNumber,
            CachedBalance = account.CachedBalance,
            LedgerBalance = ledgerBalance,
            IsReconciled = isReconciled,
            TotalEntries = entryCount
        };
    }

    public async Task ValidateAccountOwnership(Guid userId, Guid accountId)
    {
        var owns = await _db.Accounts.AnyAsync(a => a.Id == accountId && a.UserId == userId);
        if (!owns)
            throw new UnauthorizedAccountAccessException();
    }

    private static string GenerateAccountNumber(string prefix)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return $"{prefix}-{datePart}-{randomPart}";
    }

    private static AccountResponse MapAccountResponse(Account account) => new()
    {
        Id = account.Id,
        AccountNumber = account.AccountNumber,
        Type = account.Type.ToString(),
        Status = account.Status.ToString(),
        Balance = account.CachedBalance,
        Currency = account.Currency,
        CreatedAt = account.CreatedAt
    };
}
