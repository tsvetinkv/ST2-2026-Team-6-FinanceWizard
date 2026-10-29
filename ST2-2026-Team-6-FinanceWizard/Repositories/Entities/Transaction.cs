namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities
{
    /// <summary>
    /// A single money movement (income or expense) on one wallet.
    ///
    /// Design notes:
    /// - Type is a string ("Income" / "Expense"), not an enum, so EF migrations do not require
    ///   a schema change if we add a new type later. The ViewModel restricts it to these two.
    /// - Merchant / Category are nullable because a transaction can have no merchant or category.
    /// - TransactionTags is the join collection for the many-to-many relation to Tag.
    /// - The balance side-effect (Wallet.Balance +/- Amount) is applied by TransactionService
    ///   in the same SaveChanges, so the pair (transaction row, wallet row) is always consistent.
    /// </summary>
    public class Transaction
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Description { get; set; }

        public string Type { get; set; } = "Expense"; // "Income" or "Expense"

        public long WalletId { get; set; }
        public Wallet Wallet { get; set; } = null!;

        // Exactly one merchant / one category (both optional)
        public long? MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        public long? CategoryId { get; set; }
        public Category? Category { get; set; }

        // Many tags
        public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
    }
}
