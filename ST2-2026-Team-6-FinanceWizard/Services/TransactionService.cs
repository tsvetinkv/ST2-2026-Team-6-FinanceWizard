using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IEfRepository<Transaction> _transactionRepo;
        private readonly IWalletService _walletService;

        public TransactionService(
            IEfRepository<Transaction> transactionRepo,
            IWalletService walletService)
        {
            _transactionRepo = transactionRepo;
            _walletService = walletService;
        }

        public async Task<IEnumerable<Transaction>> GetAllByUserIdAsync(long userId)
        {
            return await _transactionRepo.All()
                .Where(t => t.Wallet.UserId == userId)
                .Include(t => t.Wallet)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<Transaction?> GetByIdAsync(long id, long userId)
        {
            return await _transactionRepo.All()
                .Include(t => t.Wallet)
                .FirstOrDefaultAsync(t => t.Id == id && t.Wallet.UserId == userId);
        }

        public async Task<Transaction> CreateTransactionAsync(decimal amount, string description, string type, DateTime date, long userId)
        {
            var wallet = await _walletService.GetByUserIdAsync(userId);
            if (wallet == null)
            {
                throw new InvalidOperationException("Wallet not found for this user.");
            }

            bool isIncome = string.Equals(type, "Income", StringComparison.OrdinalIgnoreCase);

            if (!isIncome && wallet.Balance < amount)
            {
                throw new InvalidOperationException(
                    $"Insufficient funds. Available balance: {wallet.Balance:F2} {wallet.Currency}");
            }

            var transaction = new Transaction
            {
                WalletId = wallet.Id,
                Amount = amount,
                Description = description,
                Type = isIncome ? "Income" : "Expense",
                Date = date
            };

            await _transactionRepo.AddAsync(transaction);
            await _transactionRepo.SaveChangesAsync();

            decimal newBalance = isIncome ? wallet.Balance + amount : wallet.Balance - amount;
            await _walletService.UpdateBalanceAsync(userId, newBalance);

            return transaction;
        }
    }
}
