using Microsoft.EntityFrameworkCore;
using CoreBank.Domain.Entities;

namespace CoreBank.Data;

/// <summary>
/// EF Core DbContext implementing the core banking schema.
/// 
/// Key design decisions:
/// - LedgerEntries are append-only (no update/delete in application code)
/// - Account.CachedBalance uses ConcurrencyCheck for optimistic locking
/// - IdempotencyRecords have unique index on (Key, UserId) for duplicate detection
/// - All monetary columns use decimal(18,2) matching banking precision requirements
/// - Foreign keys enforce referential integrity at the database level
/// </summary>
public class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== USER =====
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.NationalIdNumber).IsRequired().HasMaxLength(20);
            entity.HasIndex(u => u.NationalIdNumber).IsUnique();
        });

        // ===== ACCOUNT =====
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.AccountNumber).IsUnique();
            entity.Property(a => a.AccountNumber).IsRequired().HasMaxLength(30);
            entity.Property(a => a.CachedBalance).HasColumnType("decimal(18,2)");
            entity.Property(a => a.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("USD");
            
            // Optimistic concurrency: EF Core will include RowVersion in UPDATE WHERE clause.
            // If another transaction modified the row, SaveChanges throws DbUpdateConcurrencyException.
            entity.Property(a => a.RowVersion).IsConcurrencyToken();

            entity.HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Never cascade-delete accounts
        });

        // ===== LEDGER ENTRY =====
        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.ToTable("LedgerEntries");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(l => l.BalanceAfter).HasColumnType("decimal(18,2)");
            entity.Property(l => l.Description).HasMaxLength(500);

            // Index for fast balance calculation and transaction history queries
            entity.HasIndex(l => new { l.AccountId, l.CreatedAt });
            entity.HasIndex(l => l.TransferId);

            entity.HasOne(l => l.Account)
                .WithMany(a => a.LedgerEntries)
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Transfer)
                .WithMany(t => t.LedgerEntries)
                .HasForeignKey(l => l.TransferId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        });

        // ===== TRANSFER =====
        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.ToTable("Transfers");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(t => t.Currency).IsRequired().HasMaxLength(3);
            entity.Property(t => t.Description).HasMaxLength(500);
            entity.Property(t => t.IdempotencyKey).HasMaxLength(100);
            entity.Property(t => t.FailureReason).HasMaxLength(500);

            // Unique index on IdempotencyKey when not null - prevents duplicate transfers
            entity.HasIndex(t => t.IdempotencyKey)
                .IsUnique()
                .HasFilter(null); // SQLite doesn't support filtered indexes well

            entity.HasOne(t => t.SourceAccount)
                .WithMany()
                .HasForeignKey(t => t.SourceAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.DestinationAccount)
                .WithMany()
                .HasForeignKey(t => t.DestinationAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== IDEMPOTENCY =====
        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("IdempotencyRecords");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Key).IsRequired().HasMaxLength(100);
            entity.Property(i => i.OperationPath).IsRequired().HasMaxLength(200);
            
            // Composite unique index: idempotency is scoped per user
            entity.HasIndex(i => new { i.Key, i.UserId }).IsUnique();
        });
    }
}
