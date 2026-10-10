namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts
{
    public interface ITransactionService
    {
        Task<IEnumerable<Transaction>> GetAllByUserIdAsync(long userId);

        Task<Transaction?> GetByIdAsync(long id, long userId);

        Task<Transaction> CreateTransactionAsync(decimal amount, string description, string type, DateTime date, long userId);

        Task DeleteTransactionAsync(long id, long userId);
    }
}