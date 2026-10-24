using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts
{
    public interface ITransactionService
    {
        Task<IEnumerable<Transaction>> GetAllByUserIdAsync(long userId);

        Task<Transaction?> GetByIdAsync(long id, long userId);

        // One wallet, optional single merchant, optional single category, any number of tags
        Task<Transaction> CreateTransactionAsync(
            long userId,
            long walletId,
            decimal amount,
            string description,
            string type,
            DateTime date,
            long? merchantId,
            long? categoryId,
            IEnumerable<long> tagIds);

        // Changes an existing transaction and keeps the wallet balances correct
        Task UpdateTransactionAsync(
            long id,
            long userId,
            long walletId,
            decimal amount,
            string description,
            string type,
            DateTime date,
            long? merchantId,
            long? categoryId,
            IEnumerable<long> tagIds);

        Task DeleteTransactionAsync(long id, long userId);
    }
}
