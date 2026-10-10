using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    public class WalletService : IWalletService
    {
        private readonly IEfRepository<Wallet> _walletRepo;

        public WalletService(IEfRepository<Wallet> walletRepo)
        {
            _walletRepo = walletRepo;
        }

        public async Task<Wallet?> GetByUserIdAsync(long userId)
        {
            return await _walletRepo.All().FirstOrDefaultAsync(w => w.UserId == userId);
        }

        public async Task CreateWalletAsync(decimal initialBalance, string currency, long userId)
        {
            var wallet = new Wallet
            {
                UserId = userId,
                Balance = initialBalance,
                Currency = currency
            };

            await _walletRepo.AddAsync(wallet);
            await _walletRepo.SaveChangesAsync();
        }

        public async Task<bool> HasSufficientFundsAsync(long userId, decimal amount)
        {
            var wallet = await GetByUserIdAsync(userId);
            return wallet != null && wallet.Balance >= amount;
        }

        public async Task UpdateBalanceAsync(long userId, decimal newBalance)
        {
            if (newBalance < 0)
            {
                throw new ArgumentException("The wallet balance cannot be negative.");
            }

            var wallet = await GetByUserIdAsync(userId);
            if (wallet == null)
            {
                throw new InvalidOperationException("The user's wallet was not found.");
            }

            wallet.Balance = newBalance;
            await _walletRepo.SaveChangesAsync();
        }
    }
}
