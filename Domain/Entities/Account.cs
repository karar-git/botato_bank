using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CoreBank.Domain.Enums;

namespace CoreBank.Domain.Entities;

/// <summary>
/// Account entity - checking or savings.
/// 
/// CRITICAL DESIGN DECISION:
/// The CachedBalance field is a MATERIALIZED VIEW of the ledger sum, NOT the source of truth.
/// The true balance is ALWAYS derived by: SELECT SUM(Amount) FROM LedgerEntries WHERE AccountId = @id
/// CachedBalance exists solely for read performance and is updated atomically within the same
/// database transaction as ledger writes. It must NEVER be modified directly.
/// 
/// The RowVersion field enables optimistic concurrency control - EF Core will include it in
/// UPDATE WHERE clauses, causing concurrent modifications to fail with DbUpdateConcurrencyException.
/// </summary>
public class Account
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable account number (e.g., "CHK-20260206-A1B2C3").
    /// Unique, indexed, used for external references.
    /// </summary>
    [Required, MaxLength(30)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public AccountType Type { get; set; }

    [Required]
    public AccountStatus Status { get; set; } = AccountStatus.Active;

    /// <summary>
    /// Cached balance for read performance. 
    /// NEVER the source of truth - always reconcilable against ledger.
    /// Updated ONLY within the same transaction as ledger entry creation.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CachedBalance { get; set; } = 0m;

    [Required, MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token. EF Core auto-increments on save.
    /// Prevents lost-update anomalies when two transactions read the same balance.
    /// </summary>
    [ConcurrencyCheck]
    public long RowVersion { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}
