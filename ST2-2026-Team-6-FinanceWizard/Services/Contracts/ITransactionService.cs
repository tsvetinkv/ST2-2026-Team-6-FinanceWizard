using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts
{
    /// <summary>
    /// Contract for transaction business logic.
    ///
    /// Design notes:
    /// - The wallet balance changes are part of the "create / update / delete" contract.
    ///   Callers do not need to (and must not) adjust Wallet.Balance themselves.
    /// - Invalid requests (wallet not found, insufficient funds, missing merchant/category/tag)
    ///   are signalled with InvalidOperationException. The controller translates it to ModelState.
    /// - Every method is user-scoped: pass the caller's userId so the implementation can restrict
    ///   the query to the caller's own wallets. This is the security boundary for the whole service.
    /// </summary>
    public interface ITransactionService
    {
        /// <summary>
        /// All transactions of the user's wallets, newest first, with related entities loaded.
        /// Used by the AI assistant to build its prompt context.
        /// </summary>
        Task<IEnumerable<Transaction>> GetAllByUserIdAsync(long userId);

        /// <summary>
        /// Returns the transaction with the given id, but only if it belongs to one of the user's wallets.
        /// Returns null otherwise (which protects against id enumeration).
        /// </summary>
        Task<Transaction?> GetByIdAsync(long id, long userId);

        /// <summary>
        /// Creates a transaction and applies the balance change (income adds, expense subtracts).
        /// Throws InvalidOperationException when the wallet is not owned by the user,
        /// a referenced merchant/category/tag does not exist, or an expense would make the balance negative.
        /// </summary>
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

        /// <summary>
        /// Updates an existing transaction and repairs the balance of the old AND new wallet.
        /// Throws InvalidOperationException under the same conditions as CreateTransactionAsync
        /// (plus: the balance of either wallet must not become negative as a result).
        /// </summary>
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

        /// <summary>
        /// Deletes the transaction and reverses its balance effect on the wallet.
        /// Throws InvalidOperationException when deleting a historical income would make
        /// the wallet balance negative.
        /// </summary>
        Task DeleteTransactionAsync(long id, long userId);
    }
}
