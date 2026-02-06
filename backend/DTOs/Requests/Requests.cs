using System.ComponentModel.DataAnnotations;

namespace CoreBank.DTOs.Requests;

public class RegisterRequest
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class CreateAccountRequest
{
    [Required]
    public string Type { get; set; } = "Checking"; // "Checking" or "Savings"
}

public class DepositRequest
{
    [Required, Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be between 0.01 and 1,000,000,000")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional idempotency key to prevent duplicate deposits on retry.
    /// </summary>
    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }
}

public class WithdrawRequest
{
    [Required, Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be between 0.01 and 1,000,000,000")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }
}

public class TransferRequest
{
    [Required]
    public string SourceAccountNumber { get; set; } = string.Empty;

    [Required]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    [Required, Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be between 0.01 and 1,000,000,000")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// REQUIRED for transfers. Client must provide a unique key per transfer intent.
    /// If a retry sends the same key, the original result is returned without re-execution.
    /// </summary>
    [Required, MaxLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class TransactionHistoryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Type { get; set; } // Filter by transaction type
}
