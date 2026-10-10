namespace ST2_2026_Team_6_FinanceWizard.Repositories
{
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
