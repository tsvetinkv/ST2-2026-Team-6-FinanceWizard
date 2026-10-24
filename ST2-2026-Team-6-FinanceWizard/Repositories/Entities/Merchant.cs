namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

public class Merchant
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Relative path of the logo, e.g. "uploads/merchants/abc123.png"; null = no logo.
    // The logo is either an uploaded photo or an image downloaded from a link.
    public string? LogoUrl { get; set; }
}
