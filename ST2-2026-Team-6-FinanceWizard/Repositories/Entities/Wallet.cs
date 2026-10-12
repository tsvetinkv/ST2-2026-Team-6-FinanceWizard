namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

public static class WalletTypes
{
    public const string Spending = "Spending";
    public const string Savings = "Savings";

    public static readonly string[] All = { Spending, Savings };
}

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
