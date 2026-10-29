namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

/// <summary>
/// The two wallet kinds. Using constants instead of an enum keeps the DB column a plain string,
/// which is easier to migrate later (new types do not need a schema change).
/// </summary>
public static class WalletTypes
{
    public const string Spending = "Spending";
    public const string Savings = "Savings";

    public static readonly string[] All = { Spending, Savings };
}

/// <summary>
/// A wallet represents one pot of money (spending account, savings account, ...).
///
/// Design notes:
/// - Balance is a denormalised value (sum of initial balance + all income - all expenses).
///   It is updated together with each transaction in TransactionService.SaveChangesAsync,
///   so the pair (transaction, wallet.Balance) is always consistent.
/// - Currency is the ISO code (e.g. "EUR"). The Money helper in UiHelpers.cs turns it
///   into a symbol for display.
/// </summary>
public class Wallet
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "EUR";

    // "Spending" or "Savings"
    public string Type { get; set; } = WalletTypes.Spending;

    public long UserId { get; set; }
    public User? User { get; set; }
}
