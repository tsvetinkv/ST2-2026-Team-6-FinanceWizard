using Microsoft.EntityFrameworkCore;

namespace ST2_2026_Team_6_FinanceWizard.Repositories;

/// <summary>
/// Thread-safe Singleton holding global connection parameters while keeping ApplicationDbContext scoped.
///
/// Design pattern: SINGLETON.
/// - Purpose: there is exactly one connection-string holder in the app.
/// - Why not put the connection string in a scoped service? Because DbContextOptions must be
///   created once (they are effectively immutable) and then reused for every request.
/// - The instance is created lazily with Lazy<DatabaseConnection>, which is thread-safe
///   by default, so two concurrent first calls cannot create two instances.
/// - The private constructor prevents "new DatabaseConnection()" from outside.
/// - Initialize() only writes the connection string the first time; later calls are ignored
///   (the app should never switch DBs at runtime).
/// </summary>
public sealed class DatabaseConnection
{
    private static readonly Lazy<DatabaseConnection> _instance =
        new(() => new DatabaseConnection());

    public static DatabaseConnection Instance => _instance.Value;

    public string ConnectionString { get; private set; } = string.Empty;

    // Private constructor: only the Lazy factory above can call this.
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

    /// <summary>
    /// Creates DbContextOptions once. The caller (Program.cs) registers the context as Scoped
    /// with these options, so each request gets its own DbContext — safe for concurrency.
    /// </summary>
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
