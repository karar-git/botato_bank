using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    /// National ID number extracted from البطاقة الوطنية via OCR during registration.
    /// </summary>
    [Required, MaxLength(20)]
    public string NationalIdNumber { get; set; } = string.Empty;

    /// <summary>
    /// Whether the national ID has been verified via OCR.
    /// </summary>
    public bool IsIdVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}
