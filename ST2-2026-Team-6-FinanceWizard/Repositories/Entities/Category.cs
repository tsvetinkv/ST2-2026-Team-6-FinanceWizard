namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

public class Category
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    // Any colour picked by the user as "#rrggbb"; null = automatic colour from the name
    public string? Color { get; set; }
}