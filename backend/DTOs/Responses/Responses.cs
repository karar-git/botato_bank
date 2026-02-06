using CoreBank.Domain.Enums;

namespace CoreBank.DTOs.Responses;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserResponse User { get; set; } = null!;
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string KycStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AccountResponse
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class LedgerEntryResponse
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal BalanceAfter { get; set; }
    public Guid? TransferId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TransferResponse
{
    public Guid Id { get; set; }
    public string SourceAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }
}

public class BalanceReconciliationResponse
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal CachedBalance { get; set; }
    public decimal LedgerBalance { get; set; }
    public bool IsReconciled { get; set; }
    public int TotalEntries { get; set; }
    public DateTime ReconciledAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Extended user info for admin/employee review, includes ID card images and KYC status.
/// </summary>
public class AdminUserResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string KycStatus { get; set; } = string.Empty;
    public string? NationalIdNumber { get; set; }
    public string? RejectionReason { get; set; }
    public string? IdCardFrontImage { get; set; }
    public string? IdCardBackImage { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// System-wide statistics for admin dashboard (read-only oversight).
/// </summary>
public class SystemStatsResponse
{
    public int TotalUsers { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalMerchants { get; set; }
    public int TotalEmployees { get; set; }
    public int KycPending { get; set; }
    public int KycVerified { get; set; }
    public int KycRejected { get; set; }
    public int TotalAccounts { get; set; }
    public int ActiveAccounts { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalTransfers { get; set; }
    public decimal TotalDepositVolume { get; set; }
    public decimal TotalWithdrawalVolume { get; set; }
    public decimal TotalTransferVolume { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of a single CSV row processed by the Employee CSV upload.
/// </summary>
public class CsvRowResult
{
    public int RowNumber { get; set; }
    public string NationalId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Operation { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? AccountNumber { get; set; }
    public decimal? BalanceAfter { get; set; }
}

/// <summary>
/// Response for the Employee CSV upload endpoint.
/// </summary>
public class CsvUploadResponse
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<CsvRowResult> Results { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
