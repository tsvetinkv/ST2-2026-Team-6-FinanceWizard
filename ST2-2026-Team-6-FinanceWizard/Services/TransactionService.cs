using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IEfRepository<Transaction> _transactionRepo;
        private readonly IEfRepository<TransactionTag> _transactionTagRepo;
        private readonly IEfRepository<Merchant> _merchantRepo;
        private readonly IEfRepository<Category> _categoryRepo;
        private readonly IEfRepository<Tag> _tagRepo;
        private readonly IWalletService _walletService;

        public TransactionService(
            IEfRepository<Transaction> transactionRepo,
            IEfRepository<TransactionTag> transactionTagRepo,
            IEfRepository<Merchant> merchantRepo,
            IEfRepository<Category> categoryRepo,
            IEfRepository<Tag> tagRepo,
            IWalletService walletService)
        {
            _transactionRepo = transactionRepo;
            _transactionTagRepo = transactionTagRepo;
            _merchantRepo = merchantRepo;
            _categoryRepo = categoryRepo;
            _tagRepo = tagRepo;
            _walletService = walletService;
        }

        public async Task<IEnumerable<Transaction>> GetAllByUserIdAsync(long userId)
        {
            return await _transactionRepo.All()
                .Where(t => t.Wallet.UserId == userId)
                .Include(t => t.Wallet)
                .Include(t => t.Merchant)
                .Include(t => t.Category)
                .Include(t => t.TransactionTags).ThenInclude(tt => tt.Tag)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<Transaction?> GetByIdAsync(long id, long userId)
        {
            return await _transactionRepo.All()
                .Include(t => t.Wallet)
                .Include(t => t.TransactionTags)
                .FirstOrDefaultAsync(t => t.Id == id && t.Wallet.UserId == userId);
        }

        public async Task<Transaction> CreateTransactionAsync(
            long userId,
            long walletId,
            decimal amount,
            string description,
            string type,
            DateTime date,
            long? merchantId,
            long? categoryId,
            IEnumerable<long> tagIds)
        {
            // Wallet must belong to this user
            var wallet = await _walletService.GetByIdAsync(walletId, userId);
            if (wallet == null)
            {
                throw new InvalidOperationException("The selected wallet was not found.");
            }

            var distinctTagIds = (tagIds ?? Enumerable.Empty<long>()).Distinct().ToList();
            await EnsureReferencesExistAsync(merchantId, categoryId, distinctTagIds);

            bool isIncome = IsIncome(type);

            if (!isIncome && wallet.Balance < amount)
            {
                throw new InvalidOperationException(
                    $"Insufficient funds in \"{wallet.Name}\". Available balance: {wallet.Balance:F2} {wallet.Currency}");
            }

            var transaction = new Transaction
            {
                WalletId = wallet.Id,
                Amount = amount,
                Description = description,
                Type = isIncome ? "Income" : "Expense",
                Date = date,
                MerchantId = merchantId,
                CategoryId = categoryId,
                TransactionTags = distinctTagIds.Select(id => new TransactionTag { TagId = id }).ToList()
            };

            // The wallet is tracked by the same DbContext, so one save applies the
            // new transaction, its tags and the new balance together (all or nothing).
            wallet.Balance = isIncome ? wallet.Balance + amount : wallet.Balance - amount;

            await _transactionRepo.AddAsync(transaction);
            await _transactionRepo.SaveChangesAsync();

            return transaction;
        }

        public async Task UpdateTransactionAsync(
            long id,
            long userId,
            long walletId,
            decimal amount,
            string description,
            string type,
            DateTime date,
            long? merchantId,
            long? categoryId,
            IEnumerable<long> tagIds)
        {
            // Only finds the transaction if it belongs to one of this user's wallets
            var transaction = await GetByIdAsync(id, userId)
                ?? throw new InvalidOperationException("Transaction not found.");

            var newWallet = await _walletService.GetByIdAsync(walletId, userId)
                ?? throw new InvalidOperationException("The selected wallet was not found.");

            var distinctTagIds = (tagIds ?? Enumerable.Empty<long>()).Distinct().ToList();
            await EnsureReferencesExistAsync(merchantId, categoryId, distinctTagIds);

            var oldWallet = transaction.Wallet;
            bool wasIncome = IsIncome(transaction.Type);
            bool isIncome = IsIncome(type);

            // Balance of the old wallet with the ORIGINAL transaction taken back out
            decimal oldWalletWithoutOriginal = wasIncome
                ? oldWallet.Balance - transaction.Amount
                : oldWallet.Balance + transaction.Amount;

            if (newWallet.Id == oldWallet.Id)
            {
                // Same wallet: only the final balance has to stay at or above zero
                decimal newBalance = isIncome
                    ? oldWalletWithoutOriginal + amount
                    : oldWalletWithoutOriginal - amount;

                if (newBalance < 0)
                {
                    throw new InvalidOperationException(
                        $"Insufficient funds in \"{oldWallet.Name}\". Available balance: {oldWalletWithoutOriginal:F2} {oldWallet.Currency}");
                }

                oldWallet.Balance = newBalance;
            }
            else
            {
                // Moving to another wallet: the original must come off the old wallet
                // and the new amount must fit in the new wallet
                if (oldWalletWithoutOriginal < 0)
                {
                    throw new InvalidOperationException(
                        $"This transaction cannot be moved because the balance of \"{oldWallet.Name}\" would become negative.");
                }

                decimal newWalletBalance = isIncome
                    ? newWallet.Balance + amount
                    : newWallet.Balance - amount;

                if (newWalletBalance < 0)
                {
                    throw new InvalidOperationException(
                        $"Insufficient funds in \"{newWallet.Name}\". Available balance: {newWallet.Balance:F2} {newWallet.Currency}");
                }

                oldWallet.Balance = oldWalletWithoutOriginal;
                newWallet.Balance = newWalletBalance;
            }

            transaction.Wallet = newWallet;
            transaction.WalletId = newWallet.Id;
            transaction.Amount = amount;
            transaction.Description = description;
            transaction.Type = isIncome ? "Income" : "Expense";
            transaction.Date = date;
            transaction.MerchantId = merchantId;
            transaction.CategoryId = categoryId;

            // Tags: remove the links that are no longer chosen, add the new ones
            var existingLinks = await _transactionTagRepo.All()
                .Where(tt => tt.TransactionId == id)
                .ToListAsync();

            foreach (var link in existingLinks.Where(l => !distinctTagIds.Contains(l.TagId)))
            {
                _transactionTagRepo.Delete(link);
            }

            foreach (var tagId in distinctTagIds.Where(tagId => existingLinks.All(l => l.TagId != tagId)))
            {
                await _transactionTagRepo.AddAsync(new TransactionTag { TransactionId = id, TagId = tagId });
            }

            // Balances, the transaction and its tags are saved together (all or nothing)
            await _transactionRepo.SaveChangesAsync();
        }

        public async Task DeleteTransactionAsync(long id, long userId)
        {
            // Only finds the transaction if it belongs to one of this user's wallets
            var transaction = await GetByIdAsync(id, userId);
            if (transaction == null)
            {
                throw new InvalidOperationException("Transaction not found.");
            }

            var wallet = transaction.Wallet;
            bool isIncome = IsIncome(transaction.Type);

            // Reverse the original effect: removing income lowers the balance, removing an expense raises it
            decimal newBalance = isIncome
                ? wallet.Balance - transaction.Amount
                : wallet.Balance + transaction.Amount;

            if (newBalance < 0)
            {
                throw new InvalidOperationException(
                    $"This income cannot be deleted because the balance of \"{wallet.Name}\" would become negative. Available balance: {wallet.Balance:F2} {wallet.Currency}");
            }

            // Tag links are removed by the database (cascade delete).
            wallet.Balance = newBalance;
            _transactionRepo.Delete(transaction);
            await _transactionRepo.SaveChangesAsync();
        }

        private static bool IsIncome(string? type) =>
            string.Equals(type, "Income", StringComparison.OrdinalIgnoreCase);

        private async Task EnsureReferencesExistAsync(long? merchantId, long? categoryId, IReadOnlyCollection<long> tagIds)
        {
            if (merchantId.HasValue && !await _merchantRepo.All().AnyAsync(m => m.Id == merchantId.Value))
            {
                throw new InvalidOperationException("The selected merchant was not found.");
            }

            if (categoryId.HasValue && !await _categoryRepo.All().AnyAsync(c => c.Id == categoryId.Value))
            {
                throw new InvalidOperationException("The selected category was not found.");
            }

            if (tagIds.Count > 0)
            {
                int existing = await _tagRepo.All().CountAsync(t => tagIds.Contains(t.Id));
                if (existing != tagIds.Count)
                {
                    throw new InvalidOperationException("One or more selected tags were not found.");
                }
            }
        }
    }
}
