using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CoreBank.Domain.Enums;

namespace CoreBank.Domain.Entities;

/// <summary>
/// User entity - identity only. 
/// A User never holds a balance directly; balances belong to Accounts.
/// </summary>
public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User role: Customer, Merchant, Employee, Admin.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Customer;

    /// <summary>
    /// National ID number (رقم البطاقة الوطنية).
    /// Used for CSV bulk operations to identify users.
    /// </summary>
    [MaxLength(50)]
    public string? NationalIdNumber { get; set; }

    /// <summary>
    /// Base64-encoded front image of the national ID card (البطاقة الوطنية).
    /// Stored in DB for KYC review.
    /// </summary>
    public string? IdCardFrontImage { get; set; }

    /// <summary>
    /// Base64-encoded back image of the national ID card (البطاقة الوطنية).
    /// </summary>
    public string? IdCardBackImage { get; set; }

    /// <summary>
    /// KYC verification status. Gates sensitive operations (transfers, withdrawals).
    /// Pending = awaiting review, Verified = approved, Rejected = denied.
    /// </summary>
    public KycStatus KycStatus { get; set; } = KycStatus.Pending;

    /// <summary>
    /// Optional rejection reason if KYC was rejected.
    /// </summary>
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}
