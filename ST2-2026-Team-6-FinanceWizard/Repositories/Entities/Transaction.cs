using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

public class Transaction
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }

    public string Type { get; set; } = "Expense"; // "Income" or "Expense"

    public long WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;
<<<<<<< HEAD
=======

    // Exactly one merchant / one category (both optional)
    public long? MerchantId { get; set; }
    public Merchant? Merchant { get; set; }

    public long? CategoryId { get; set; }
    public Category? Category { get; set; }

    // Many tags
    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
>>>>>>> origin/feature/database-setup
}
