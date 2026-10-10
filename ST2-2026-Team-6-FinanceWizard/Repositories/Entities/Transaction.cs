using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

public class Transaction
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }

    public string Type { get; set; } = "Expense"; // "Income" или "Expense"

    public long WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;
}