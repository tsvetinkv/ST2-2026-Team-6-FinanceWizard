namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

public class Wallet
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "EUR";

    public long UserId { get; set; }
    public User? User { get; set; }
}