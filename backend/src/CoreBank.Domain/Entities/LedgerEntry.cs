using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CoreBank.Domain.Enums;

namespace CoreBank.Domain.Entities;

/// <summary>
/// Immutable ledger entry - the ONLY source of truth for money movement.
/// 
/// Every monetary operation (deposit, withdrawal, transfer) creates one or more LedgerEntries.
/// Entries are NEVER updated or deleted - they are append-only.
/// 
/// For transfers: two entries are created atomically (debit + credit) linked by TransferId.
/// For deposits: one positive entry.
/// For withdrawals: one negative entry.
/// 
/// The account balance at any point = SUM(Amount) of all ledger entries for that account.
/// </summary>
public class LedgerEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AccountId { get; set; }

    /// <summary>
    /// Positive = money in (deposit, credit side of transfer).
    /// Negative = money out (withdrawal, debit side of transfer).
    /// </summary>
    [Required, Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;

    /// <summary>
    /// Running balance AFTER this entry was applied.
    /// Stored for fast statement generation and auditability.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Links debit + credit legs of a transfer. Null for deposits/withdrawals.
    /// </summary>
    public Guid? TransferId { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Immutable creation timestamp. Entries are never modified.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }

    [ForeignKey(nameof(TransferId))]
    public Transfer? Transfer { get; set; }
}
