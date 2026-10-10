namespace ST2_2026_Team_6_FinanceWizard.Repositories 
{
    public interface IEfRepository<T> where T : class
    {
        // --- Четене ---
        IQueryable<T> All();
        Task<T> GetByIdAsync(object id);

        // --- Писане ---
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);

        // --- Запазване ---
        Task<int> SaveChangesAsync();
    }
}