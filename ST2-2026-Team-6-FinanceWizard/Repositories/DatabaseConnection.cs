using Microsoft.EntityFrameworkCore;

namespace ST2_2026_Team_6_FinanceWizard.Repositories;

/// <summary>
/// Thread-safe Singleton holding global connection parameters while keeping ApplicationDbContext scoped.
/// </summary>
public sealed class DatabaseConnection
{
    private static readonly Lazy<DatabaseConnection> _instance =
        new(() => new DatabaseConnection());

    public static DatabaseConnection Instance => _instance.Value;

    public string ConnectionString { get; private set; } = string.Empty;

    private DatabaseConnection() { }

    public void Initialize(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");
        }

        // Fix: Use IsNullOrEmpty check instead of ??= because string.Empty is non-null
        if (string.IsNullOrEmpty(ConnectionString))
        {
            ConnectionString = connectionString;
        }
    }

    public DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            throw new InvalidOperationException("DatabaseConnection must be initialized before creating options.");
        }

        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
    }
}