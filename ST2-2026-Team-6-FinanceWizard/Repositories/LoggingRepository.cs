namespace ST2_2026_Team_6_FinanceWizard.Repositories
{
    /// <summary>
    /// Дизайн патърн: DECORATOR.
    /// Обвива EfRepository<T> през същия интерфейс IEfRepository<T> и добавя
    /// логване на всички операции по запис, без да променя EfRepository или услугите.
    /// Регистриран в Program.cs като имплементация на IEfRepository<T>.
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

        public IQueryable<T> All() => _inner.All();

        public Task<T?> GetByIdAsync(object id) => _inner.GetByIdAsync(id);

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