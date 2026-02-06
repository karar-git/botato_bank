using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreBank.Domain.Entities;

/// <summary>
/// Idempotency record - prevents duplicate execution of the same operation.
/// 
/// When a client sends a request with an IdempotencyKey header:
///   1. We check if a record exists for that key
///   2. If yes and completed: return the cached response (no re-execution)
///   3. If yes and in-progress: return 409 Conflict
///   4. If no: create record, execute operation, store result
/// 
/// This is critical for financial operations where network failures can cause
/// client retries. Without idempotency, a retry could execute a transfer twice.
/// </summary>
public class IdempotencyRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Client-provided unique key for this operation.
    /// </summary>
    [Required, MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The user who initiated this operation.
    /// Scoped per-user to prevent key collisions across users.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// HTTP method + path that was called (e.g., "POST /api/transfers").
    /// </summary>
    [Required, MaxLength(200)]
    public string OperationPath { get; set; } = string.Empty;

    /// <summary>
    /// Serialized JSON of the original request body.
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// HTTP status code of the cached response.
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Serialized JSON of the response body.
    /// </summary>
    public string? ResponseBody { get; set; }

    public bool IsCompleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
