using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CoreBank.Domain.Enums;

namespace CoreBank.Domain.Entities;

/// <summary>
/// Transfer entity - links the debit and credit legs of a fund transfer.
/// 
/// A transfer always produces exactly TWO ledger entries:
///   1. Debit entry on the source account (negative amount)
///   2. Credit entry on the destination account (positive amount)
/// 
/// Both entries share the same TransferId, enabling audit trail reconstruction.
/// The transfer record itself is the "journal entry" in accounting terms.
/// </summary>
public class Transfer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SourceAccountId { get; set; }

    [Required]
    public Guid DestinationAccountId { get; set; }

    [Required, Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    public TransferStatus Status { get; set; } = TransferStatus.Pending;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Client-provided idempotency key. Prevents duplicate transfers on retry.
    /// </summary>
    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Reason for failure, if Status == Failed.
    /// </summary>
    [MaxLength(500)]
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(SourceAccountId))]
    public Account? SourceAccount { get; set; }

    [ForeignKey(nameof(DestinationAccountId))]
    public Account? DestinationAccount { get; set; }

    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}
