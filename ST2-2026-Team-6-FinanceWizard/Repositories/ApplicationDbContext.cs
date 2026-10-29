using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Repositories;

/// <summary>
/// EF Core DbContext — the database schema and relationship configuration.
///
/// Design notes:
/// - Registered as Scoped in DI: one instance per HTTP request, shared by all repositories
///   so they participate in the same unit of work (one SaveChanges commits all pending changes together).
/// - OnModelCreating configures relationships and cascade-delete rules.
/// - Monetary columns use HasPrecision(18, 2) to avoid floating-point rounding.
/// - Cascade delete on User -> Wallets means deleting a user cleans up everything automatically.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TransactionTag> TransactionTags => Set<TransactionTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Monetary Precision Configurations
        // HasPrecision(18, 2) → max 18 digits total, 2 after the decimal point.
        modelBuilder.Entity<Wallet>()
            .Property(w => w.Balance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        // 1:N Relationship User -> Wallets (spending / savings)
        // OnDelete(Cascade): deleting a User automatically deletes their wallets.
        modelBuilder.Entity<User>()
            .HasMany(u => u.Wallets)
            .WithOne(w => w.User)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Transaction -> Merchant / Category (single, optional)
        // No cascade: deleting a merchant / category must NOT delete transactions.
        // The controllers handle this explicitly by setting MerchantId / CategoryId to null first.
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Merchant)
            .WithMany()
            .HasForeignKey(t => t.MerchantId);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId);

        // Transaction <-> Tag (many-to-many through TransactionTag)
        // Composite primary key: the pair (TransactionId, TagId) must be unique.
        modelBuilder.Entity<TransactionTag>()
            .HasKey(tt => new { tt.TransactionId, tt.TagId });

        // Cascade on the transaction side: deleting a transaction removes its tag links.
        modelBuilder.Entity<TransactionTag>()
            .HasOne(tt => tt.Transaction)
            .WithMany(t => t.TransactionTags)
            .HasForeignKey(tt => tt.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cascade on the tag side: deleting a tag removes its transaction links.
        // This is what TagController.Delete relies on.
        modelBuilder.Entity<TransactionTag>()
            .HasOne(tt => tt.Tag)
            .WithMany()
            .HasForeignKey(tt => tt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
