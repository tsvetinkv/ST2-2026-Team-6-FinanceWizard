namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

/// <summary>
/// Merchant = the "where was it bought" dimension of a transaction (e.g. Lidl, Kaufland).
///
/// Design notes:
/// - LogoUrl stores a RELATIVE path (e.g. "uploads/merchants/abc.png"), not an absolute URL.
///   This keeps the DB portable between environments (localhost vs production).
/// - null means "no logo". The file itself is managed by ILogoStorage (upload / download / delete).
/// </summary>
public class Merchant
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Relative path of the logo, e.g. "uploads/merchants/abc123.png"; null = no logo.
    // The logo is either an uploaded photo or an image downloaded from a link.
    public string? LogoUrl { get; set; }
}
