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
            var transactions = await _transactionRepo.All()
                .Where(t => t.Wallet.UserId == userId)
                .Include(t => t.Wallet)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return transactions;
        }

        public async Task<Transaction?> GetByIdAsync(long id, long userId)
        {
            var transaction = await _transactionRepo.All()
                .Include(t => t.Wallet)
                .FirstOrDefaultAsync(t => t.Id == id && t.Wallet.UserId == userId);

            return transaction;
        }

        public async Task<Transaction> CreateTransactionAsync(decimal amount, string description, string type, DateTime date, long userId)
        {
            var walletModel = await _walletService.GetByUserIdAsync(userId);
            if (walletModel == null)
            {
                throw new Exception("Wallet not found for this user.");
            }

            if (type == "Expense")
            {
                bool hasMoney = await _walletService.HasSufficientFundsAsync(userId, amount);
                if (!hasMoney)
                {
                    throw new Exception("Нямате достатъчно средства за този разход!");
                }
            }

            var transaction = new Transaction
            {
                WalletId = walletModel.Id,
                Amount = amount,
                Description = description,
                Type = type,
                Date = date
            };

            await _transactionRepo.AddAsync(transaction);
            await _transactionRepo.SaveChangesAsync();

            decimal newBalance = type == "Income"
                ? walletModel.Balance + amount
                : walletModel.Balance - amount;

            await _walletService.UpdateBalanceAsync(userId, newBalance);

            return transaction;
        }
    }
}