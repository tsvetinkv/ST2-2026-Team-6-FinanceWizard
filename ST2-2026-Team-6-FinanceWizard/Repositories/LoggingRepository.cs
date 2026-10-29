namespace ST2_2026_Team_6_FinanceWizard.Repositories
{
    /// <summary>
    /// Design pattern: DECORATOR.
    /// Wraps EfRepository{T} through the same interface IEfRepository{T} and adds
    /// logging to all write operations, without changing EfRepository or the services.
    /// Registered in Program.cs as the implementation of IEfRepository{T}.
    ///
    /// How it works:
    /// - Constructor takes an EfRepository{T} (the "inner" object) and an ILogger.
    /// - Every method delegates to the inner repository; the write methods additionally log.
    /// - Consumers see exactly IEfRepository{T} — they do not know (and do not care)
    ///   whether they are talking to EfRepository or LoggingRepository.
    /// - The decorator is registered in DI as the IEfRepository{T} implementation,
    ///   so every service automatically gets logging.
    /// </summary>
    public class LoggingRepository<T> : IEfRepository<T> where T : class
    {
        private readonly EfRepository<T> _inner;
        private readonly ILogger<LoggingRepository<T>> _logger;

        public LoggingRepository(EfRepository<T> inner, ILogger<LoggingRepository<T>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        // Reads are passed through untouched — logging reads would be too noisy.
        public IQueryable<T> All() => _inner.All();

        public Task<T?> GetByIdAsync(object id) => _inner.GetByIdAsync(id);

        // Write operations are logged before/after they run.
        public async Task AddAsync(T entity)
        {
            _logger.LogInformation("Add {Entity}", typeof(T).Name);
            await _inner.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _logger.LogInformation("Update {Entity}", typeof(T).Name);
            _inner.Update(entity);
        }

        public void Delete(T entity)
        {
            _logger.LogInformation("Delete {Entity}", typeof(T).Name);
            _inner.Delete(entity);
        }

        public async Task<int> SaveChangesAsync()
        {
            var rows = await _inner.SaveChangesAsync();
            _logger.LogInformation("SaveChanges for {Entity}: {Rows} row(s)", typeof(T).Name, rows);
            return rows;
        }
    }
}
