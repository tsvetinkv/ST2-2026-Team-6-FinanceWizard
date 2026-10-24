namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

public class Tag
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Required colour as "#rrggbb"
    public string Color { get; set; } = "#1d4ed8";

    // Optional Bootstrap Icons name (e.g. "airplane"); null = no icon
    public string? Icon { get; set; }
}
