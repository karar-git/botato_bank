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
    /// User role: Customer, Agent, Operator, Admin.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Customer;

    /// <summary>
    /// Base64-encoded front image of the national ID card (البطاقة الوطنية).
    /// Stored in DB for admin review during approval.
    /// </summary>
    public string? IdCardFrontImage { get; set; }

    /// <summary>
    /// Base64-encoded back image of the national ID card (البطاقة الوطنية).
    /// </summary>
    public string? IdCardBackImage { get; set; }

    /// <summary>
    /// Whether the user has been approved by an admin after ID review.
    /// Users cannot perform banking operations until approved.
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// Optional rejection reason if admin rejected the ID.
    /// </summary>
    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}
