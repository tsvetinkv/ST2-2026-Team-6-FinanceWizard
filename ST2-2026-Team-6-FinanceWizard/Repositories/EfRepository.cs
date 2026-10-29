using Microsoft.EntityFrameworkCore;

namespace ST2_2026_Team_6_FinanceWizard.Repositories
{
    /// <summary>
    /// Generic EF Core repository.
    ///
    /// Design pattern: REPOSITORY.
    /// - Purpose: hide DbSet usage behind an interface so controllers and services depend
    ///   on IEfRepository{T}, not on EF Core. This makes unit-testing easier (mock the interface)
    ///   and centralises data-access decisions (tracking, Include, etc.).
    /// - All() exposes IQueryable so callers can still compose LINQ queries (filter / order / Include).
    /// - It is registered as Scoped in DI so the same DbContext is shared within a request.
    ///
    /// See also LoggingRepository{T} — a DECORATOR around this class that adds log statements
    /// without modifying this file.
    /// </summary>
    public class EfRepository<T> : IEfRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public EfRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        /// <summary>
        /// Returns a queryable for the entity set. Callers can chain Where/OrderBy/Include,
        /// and the query only hits the DB when it is materialised (ToListAsync, FirstAsync, etc.).
        /// </summary>
        public IQueryable<T> All()
        {
            return _dbSet;
        }

        /// <summary>
        /// Finds an entity by its primary key. Uses the change tracker, so an already-loaded entity
        /// is returned from memory without a second SQL round-trip.
        /// </summary>
        public async Task<T?> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// Adds a new entity. The INSERT happens on SaveChangesAsync, not here.
        /// </summary>
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        /// <summary>
        /// Marks an existing entity as Modified. Useful when the entity was loaded AsNoTracking.
        /// </summary>
        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        /// <summary>
        /// Marks an entity for deletion. The DELETE happens on SaveChangesAsync.
        /// </summary>
        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        /// <summary>
        /// Commits all pending changes (inserts, updates, deletes) in a single transaction.
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
