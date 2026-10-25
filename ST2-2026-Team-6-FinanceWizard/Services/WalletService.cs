using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    public class WalletService : IWalletService
    {
        private readonly IEfRepository<Wallet> _walletRepo;
        private readonly IEfRepository<Transaction> _transactionRepo;
        private readonly IExchangeRateService _rates;

        public WalletService(
            IEfRepository<Wallet> walletRepo,
            IEfRepository<Transaction> transactionRepo,
            IExchangeRateService rates)
        {
            _walletRepo = walletRepo;
            _transactionRepo = transactionRepo;
            _rates = rates;
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

        private const string RateError = "Couldn't load the exchange rate. Try again in a few minutes.";

        public async Task<Wallet?> GetMainWalletAsync(long userId)
        {
            // Main wallet = the first wallet of the user (created at registration).
            // If you have an IsMain / IsDefault flag, filter or order by it here.
            return await _walletRepo.All()
                .Where(w => w.UserId == userId)
                .OrderBy(w => w.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<CurrencyPreview> PreviewCurrencyChangeAsync(long userId, long walletId, string targetCurrency)
        {
            var target = CurrencyInfo.Find(targetCurrency);
            if (target == null) return CurrencyPreview.Fail("Unsupported currency.");

            var wallet = await _walletRepo.All().AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == walletId && w.UserId == userId);
            if (wallet == null) return CurrencyPreview.Fail("Wallet not found.");

            var current = CurrencyInfo.Find(wallet.Currency);
            if (current == null) return CurrencyPreview.Fail("This wallet's currency can't be converted.");

            // Empty wallet: only the label changes, no rate needed
            if (!await HasMoneyOrTransactionsAsync(wallet))
                return CurrencyPreview.Ok(target.Code, current.Code, null, 0m, 0m, 1);

            var rate = await TryGetRateAsync(current.Code, target.Code);
            if (rate == null) return CurrencyPreview.Fail(RateError);

            var after = Math.Round(wallet.Balance * rate.Value, 2, MidpointRounding.AwayFromZero);
            return CurrencyPreview.Ok(target.Code, current.Code, rate, wallet.Balance, after, 1);
        }

        public async Task<CurrencyChangeResult> ChangeCurrencyAsync(long userId, long walletId, string newCurrency)
        {
            var target = CurrencyInfo.Find(newCurrency);
            if (target == null) return CurrencyChangeResult.Fail("Unsupported currency.");

            var wallet = await _walletRepo.All().FirstOrDefaultAsync(w => w.Id == walletId && w.UserId == userId);
            if (wallet == null) return CurrencyChangeResult.Fail("Wallet not found.");

            var current = CurrencyInfo.Find(wallet.Currency);
            if (current == null) return CurrencyChangeResult.Fail("This wallet's currency can't be converted.");

            if (current.Code == target.Code)
            {
                wallet.Currency = target.Code; // also normalises old symbols
                await _walletRepo.SaveChangesAsync();
                return CurrencyChangeResult.Ok(current.Code, target.Code, 1m, 1);
            }

            // No money and no transactions: nothing to convert, just switch the currency
            if (!await HasMoneyOrTransactionsAsync(wallet))
            {
                wallet.Currency = target.Code;
                await _walletRepo.SaveChangesAsync();
                return CurrencyChangeResult.Ok(current.Code, target.Code, 1m, 1) with { Converted = false };
            }

            var rate = await TryGetRateAsync(current.Code, target.Code);
            if (rate == null) return CurrencyChangeResult.Fail(RateError);

            // Balance and all transactions of THIS wallet change in one SaveChanges
            var transactions = await _transactionRepo.All().Where(t => t.WalletId == wallet.Id).ToListAsync();
            foreach (var t in transactions)
                t.Amount = ConvertAmount(t.Amount, rate.Value);

            wallet.Balance = Math.Round(wallet.Balance * rate.Value, 2, MidpointRounding.AwayFromZero);
            wallet.Currency = target.Code;

            await _walletRepo.SaveChangesAsync();
            return CurrencyChangeResult.Ok(current.Code, target.Code, rate.Value, 1);
        }

        private async Task<bool> HasMoneyOrTransactionsAsync(Wallet wallet)
        {
            if (wallet.Balance != 0) return true;
            return await _transactionRepo.All().AnyAsync(t => t.WalletId == wallet.Id);
        }

        private async Task<decimal?> TryGetRateAsync(string from, string to)
        {
            try { return await _rates.GetRateAsync(from, to); }
            catch (ExchangeRateUnavailableException) { return null; }
        }

        private static decimal ConvertAmount(decimal amount, decimal rate)
        {
            var converted = Math.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
            return amount > 0 && converted < 0.01m ? 0.01m : converted; // never turn a real amount into 0
        }
    }
}
