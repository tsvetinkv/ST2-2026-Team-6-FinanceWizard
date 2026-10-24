using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    public class WalletService : IWalletService
    {
        private readonly IEfRepository<Wallet> _walletRepo;
        private readonly IEfRepository<Transaction> _transactionRepo;

        public WalletService(IEfRepository<Wallet> walletRepo, IEfRepository<Transaction> transactionRepo)
        {
            _walletRepo = walletRepo;
            _transactionRepo = transactionRepo;
        }

        public async Task<List<Wallet>> GetAllByUserIdAsync(long userId)
        {
            return await _walletRepo.All()
                .Where(w => w.UserId == userId)
                .OrderBy(w => w.Type)
                .ThenBy(w => w.Name)
                .ToListAsync();
        }

        public async Task<Wallet?> GetByIdAsync(long walletId, long userId)
        {
            return await _walletRepo.All()
                .FirstOrDefaultAsync(w => w.Id == walletId && w.UserId == userId);
        }

        public async Task<Wallet> CreateWalletAsync(string name, string type, decimal initialBalance, string currency, long userId)
        {
            if (initialBalance < 0)
            {
                throw new ArgumentException("The wallet balance cannot be negative.");
            }

            var wallet = new Wallet
            {
                UserId = userId,
                Name = name.Trim(),
                Type = WalletTypes.All.Contains(type) ? type : WalletTypes.Spending,
                Balance = initialBalance,
                Currency = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant()
            };

            await _walletRepo.AddAsync(wallet);
            await _walletRepo.SaveChangesAsync();
            return wallet;
        }

        public async Task<Wallet?> UpdateWalletAsync(long walletId, string name, string type, decimal balance, string currency, long userId)
        {
            if (balance < 0)
            {
                throw new ArgumentException("The wallet balance cannot be negative.");
            }

            // Only a wallet that belongs to this user can be changed
            var wallet = await GetByIdAsync(walletId, userId);
            if (wallet == null)
            {
                return null;
            }

            wallet.Name = name.Trim();
            wallet.Type = WalletTypes.All.Contains(type) ? type : WalletTypes.Spending;
            wallet.Balance = balance;
            wallet.Currency = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant();

            await _walletRepo.SaveChangesAsync();
            return wallet;
        }

        public async Task<bool> DeleteWalletAsync(long walletId, long userId)
        {
            var wallet = await GetByIdAsync(walletId, userId);
            if (wallet == null)
            {
                return false;
            }

            // The database would delete the wallet's transactions silently, so refuse instead
            bool hasTransactions = await _transactionRepo.All().AnyAsync(t => t.WalletId == walletId);
            if (hasTransactions)
            {
                throw new InvalidOperationException(
                    $"\"{wallet.Name}\" still has transactions. Delete them first, then delete the wallet.");
            }

            _walletRepo.Delete(wallet);
            await _walletRepo.SaveChangesAsync();
            return true;
        }
    }
}
