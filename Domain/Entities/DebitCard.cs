using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CoreBank.Domain.Enums;

namespace CoreBank.Domain.Entities;

/// <summary>
/// Virtual debit card linked to a bank account.
/// Each account can have at most one active card.
/// Card numbers are generated as 16-digit Visa-like numbers.
/// CVV is stored hashed; only returned on creation.
/// </summary>
public class DebitCard
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// 16-digit card number (stored in full for demo; in prod would be tokenized).
    /// </summary>
    [Required, MaxLength(19)]
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>
    /// Cardholder name (user's full name, uppercased).
    /// </summary>
    [Required, MaxLength(100)]
    public string CardholderName { get; set; } = string.Empty;

    /// <summary>
    /// Expiry date stored as MMYY string (e.g., "0229" for Feb 2029).
    /// </summary>
    [Required, MaxLength(4)]
    public string ExpiryDate { get; set; } = string.Empty;

    /// <summary>
    /// CVV hash (BCrypt). The raw CVV is only returned once at creation.
    /// </summary>
    [Required]
    public string CvvHash { get; set; } = string.Empty;

    /// <summary>
    /// Card status: Active, Frozen, or Cancelled.
    /// </summary>
    public CardStatus Status { get; set; } = CardStatus.Active;

    /// <summary>
    /// Daily spending limit for the card.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DailyLimit { get; set; } = 5000m;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
