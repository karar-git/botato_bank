using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreBank.Data;
using CoreBank.Domain.Entities;
using CoreBank.Domain.Enums;
using CoreBank.DTOs.Requests;
using CoreBank.DTOs.Responses;

namespace CoreBank.Controllers;

/// <summary>
/// Admin controller.
/// - READ-ONLY for money (cannot move balances, cannot transact).
/// - CAN review and approve/reject KYC submissions.
/// - CAN view system stats, users, transaction logs.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly BankDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(BankDbContext db, ILogger<AdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ==================== SYSTEM STATS (read-only) ====================

    /// <summary>
    /// Get system-wide statistics for admin dashboard.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetSystemStats()
    {
        var stats = new SystemStatsResponse
        {
            TotalUsers = await _db.Users.CountAsync(),
            TotalCustomers = await _db.Users.CountAsync(u => u.Role == UserRole.Customer),
            TotalMerchants = await _db.Users.CountAsync(u => u.Role == UserRole.Merchant),
            TotalEmployees = await _db.Users.CountAsync(u => u.Role == UserRole.Employee),
            KycPending = await _db.Users.CountAsync(u => u.KycStatus == KycStatus.Pending),
            KycVerified = await _db.Users.CountAsync(u => u.KycStatus == KycStatus.Verified),
            KycRejected = await _db.Users.CountAsync(u => u.KycStatus == KycStatus.Rejected),
            TotalAccounts = await _db.Accounts.CountAsync(),
            ActiveAccounts = await _db.Accounts.CountAsync(a => a.Status == AccountStatus.Active),
            TotalTransactions = await _db.LedgerEntries.CountAsync(),
            TotalTransfers = await _db.Transfers.CountAsync(),
            TotalDepositVolume = await _db.LedgerEntries
                .Where(l => l.Type == TransactionType.Deposit && l.Status == TransactionStatus.Completed)
                .SumAsync(l => l.Amount),
            TotalWithdrawalVolume = await _db.LedgerEntries
                .Where(l => l.Type == TransactionType.Withdrawal && l.Status == TransactionStatus.Completed)
                .SumAsync(l => Math.Abs(l.Amount)),
            TotalTransferVolume = await _db.Transfers
                .Where(t => t.Status == TransferStatus.Completed)
                .SumAsync(t => t.Amount)
        };

        return Ok(stats);
    }

    // ==================== USER VIEWS (read-only) ====================

    /// <summary>
    /// List all users in the system.
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                KycStatus = u.KycStatus.ToString(),
                NationalIdNumber = u.NationalIdNumber,
                RejectionReason = u.RejectionReason,
                IdCardFrontImage = null, // Don't send images in list view
                IdCardBackImage = null,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Get a specific user's details including ID card images.
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new AdminUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            KycStatus = user.KycStatus.ToString(),
            NationalIdNumber = user.NationalIdNumber,
            RejectionReason = user.RejectionReason,
            IdCardFrontImage = user.IdCardFrontImage,
            IdCardBackImage = user.IdCardBackImage,
            CreatedAt = user.CreatedAt
        });
    }

    // ==================== KYC REVIEW (admin approves/rejects) ====================

    /// <summary>
    /// List all users with KYC status Pending (awaiting admin review).
    /// </summary>
    [HttpGet("pending-users")]
    public async Task<IActionResult> GetPendingUsers()
    {
        var users = await _db.Users
            .Where(u => u.KycStatus == KycStatus.Pending && u.IdCardFrontImage != null)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new AdminUserResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                KycStatus = u.KycStatus.ToString(),
                NationalIdNumber = u.NationalIdNumber,
                RejectionReason = u.RejectionReason,
                IdCardFrontImage = u.IdCardFrontImage,
                IdCardBackImage = u.IdCardBackImage,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Verify or reject a user's KYC after reviewing their ID card images.
    /// When verified, a checking account is auto-created for the user.
    /// Once KYC is set to Verified or Rejected, it is FINAL (no overrides).
    /// </summary>
    [HttpPost("users/{userId}/kyc")]
    public async Task<IActionResult> UpdateKycStatus(Guid userId, [FromBody] UpdateKycRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        // Once verified or rejected, status is final
        if (user.KycStatus != KycStatus.Pending)
            return BadRequest(new { message = $"KYC status is already {user.KycStatus}. Cannot override." });

        if (!Enum.TryParse<KycStatus>(request.Status, true, out var newStatus) ||
            newStatus == KycStatus.Pending)
            return BadRequest(new { message = "Status must be 'Verified' or 'Rejected'." });

        if (newStatus == KycStatus.Verified)
        {
            user.KycStatus = KycStatus.Verified;
            user.RejectionReason = null;
            user.UpdatedAt = DateTime.UtcNow;

            // Auto-create checking account on KYC verification
            var account = new Account
            {
                Id = Guid.NewGuid(),
                AccountNumber = GenerateAccountNumber("CHK"),
                UserId = user.Id,
                Type = AccountType.Checking,
                Status = AccountStatus.Active,
                CachedBalance = 0m,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow,
                RowVersion = 0
            };
            _db.Accounts.Add(account);

            _logger.LogInformation("Admin verified KYC for user {UserId}, created account {AccountNumber}",
                userId, account.AccountNumber);
        }
        else
        {
            user.KycStatus = KycStatus.Rejected;
            user.RejectionReason = request.RejectionReason ?? "KYC verification failed.";
            user.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Admin rejected KYC for user {UserId}: {Reason}",
                userId, user.RejectionReason);
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = newStatus == KycStatus.Verified ? "KYC verified." : "KYC rejected.",
            userId,
            kycStatus = newStatus.ToString()
        });
    }

    // ==================== TRANSACTION LOGS (read-only) ====================

    /// <summary>
    /// View recent transaction log entries (read-only oversight).
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetRecentTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var totalCount = await _db.LedgerEntries.CountAsync();

        var entries = await _db.LedgerEntries
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LedgerEntryResponse
            {
                Id = l.Id,
                AccountId = l.AccountId,
                Amount = l.Amount,
                Type = l.Type.ToString(),
                Status = l.Status.ToString(),
                BalanceAfter = l.BalanceAfter,
                TransferId = l.TransferId,
                Description = l.Description,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return Ok(new PaginatedResponse<LedgerEntryResponse>
        {
            Items = entries,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// View recent transfers (read-only oversight).
    /// </summary>
    [HttpGet("transfers")]
    public async Task<IActionResult> GetRecentTransfers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var totalCount = await _db.Transfers.CountAsync();

        var transfers = await _db.Transfers
            .Include(t => t.SourceAccount)
            .Include(t => t.DestinationAccount)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransferResponse
            {
                Id = t.Id,
                SourceAccountNumber = t.SourceAccount.AccountNumber,
                DestinationAccountNumber = t.DestinationAccount.AccountNumber,
                Amount = t.Amount,
                Currency = t.Currency,
                Status = t.Status.ToString(),
                Description = t.Description,
                IdempotencyKey = t.IdempotencyKey,
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt
            })
            .ToListAsync();

        return Ok(new PaginatedResponse<TransferResponse>
        {
            Items = transfers,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    private static string GenerateAccountNumber(string prefix)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpper();
        return $"{prefix}-{datePart}-{randomPart}";
    }
}
