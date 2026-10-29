using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    /// <summary>
    /// Business logic for creating / updating / deleting transactions.
    ///
    /// Design patterns used:
    /// - Service layer (part of the "Service" pattern): the controller has no balance-mutation logic,
    ///   it just calls these methods. This is where the "money movement" business rules live.
    /// - Transactional consistency: all balance changes are applied in the same SaveChanges
    ///   call as the transaction row, so the DB is never left in a state where the balance
    ///   does not match the sum of the transactions.
    ///
    /// Key invariant: a Wallet.Balance can never become negative. Every method checks this
    /// before applying a mutation and throws InvalidOperationException with a user-friendly message.
    /// </summary>
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

        /// <summary>
        /// All transactions of the user's wallets, newest first, with their related
        /// wallet / merchant / category / tags loaded.
        /// Used by the AI assistant to build the prompt context.
        /// </summary>
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

        /// <summary>
        /// Finds a single transaction, but only if it belongs to one of the user's wallets.
        /// The user-scoping is built into the query, so an attacker cannot enumerate
        /// other users' transaction ids.
        /// </summary>
        public async Task<Transaction?> GetByIdAsync(long id, long userId)
        {
            return await _transactionRepo.All()
                .Include(t => t.Wallet)
                .Include(t => t.TransactionTags)
                .FirstOrDefaultAsync(t => t.Id == id && t.Wallet.UserId == userId);
        }

        /// <summary>
        /// Creates a transaction and updates the wallet's balance in one SaveChanges.
        ///
        /// Steps:
        /// 1. Resolve the wallet (must belong to this user).
        /// 2. Check that merchant / category / tags exist.
        /// 3. If it's an expense, ensure the wallet has enough money.
        /// 4. Build the transaction entity and update wallet.Balance.
        /// 5. Add + SaveChanges — both changes are committed together.
        /// </summary>
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

            // De-duplicate the tag ids: the same tag must not be attached twice.
            var distinctTagIds = (tagIds ?? Enumerable.Empty<long>()).Distinct().ToList();
            await EnsureReferencesExistAsync(merchantId, categoryId, distinctTagIds);

            bool isIncome = IsIncome(type);

            // Business rule: an expense cannot make the balance negative.
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

        /// <summary>
        /// Updates a transaction and repairs both the old and the new wallet's balance.
        ///
        /// Two cases:
        /// 1. Same wallet — take the original transaction out, apply the new one, check ≥ 0.
        /// 2. Moving to another wallet — take the original out of the old wallet (must stay ≥ 0),
        ///    apply the new one to the new wallet (must stay ≥ 0).
        /// </summary>
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

            // Balance of the old wallet with the ORIGINAL transaction taken back out.
            // This is the "baseline" against which the new transaction will be applied.
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

        /// <summary>
        /// Deletes a transaction and reverses its balance effect.
        ///
        /// Reversing an income reduces the balance — that could make it negative if the money was
        /// already spent, in which case we refuse with an explanation.
        /// Reversing an expense always adds money back, so it is always safe.
        /// </summary>
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

        /// <summary>
        /// Verifies that every referenced merchant / category / tag actually exists.
        /// This turns a potential FOREIGN KEY constraint violation into a clear business error.
        /// </summary>
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
                // One query that counts how many of the submitted ids exist.
                int existing = await _tagRepo.All().CountAsync(t => tagIds.Contains(t.Id));
                if (existing != tagIds.Count)
                {
                    throw new InvalidOperationException("One or more selected tags were not found.");
                }
            }
        }
    }
}
