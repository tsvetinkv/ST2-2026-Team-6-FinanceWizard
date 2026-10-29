namespace ST2_2026_Team_6_FinanceWizard.Repositories
{
    /// <summary>
    /// Repository interface — the contract used by the rest of the app.
    ///
    /// Design pattern: REPOSITORY (interface side).
    /// - Allows a mock implementation in unit tests, and a decorator (LoggingRepository{T})
    ///   in production without changing any consumer.
    /// - The "where T : class" constraint is required because EF Core only works with reference types.
    /// </summary>
    public interface IEfRepository<T> where T : class
    {
        // --- Read ---
        IQueryable<T> All();
        Task<T?> GetByIdAsync(object id);

        // --- Write ---
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);

        // --- Persist ---
        Task<int> SaveChangesAsync();
    }
}
