namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

/// <summary>
/// Category = the "what was bought" dimension of a transaction (e.g. Shopping, Bills).
///
/// Design notes:
/// - Color is nullable: null means "use the automatic hue derived from the name" (Hue.Of),
///   while a non-null value is a user-chosen "#rrggbb".
/// - The entity has no navigation to Transactions, so deleting a category does not cascade-delete
///   anything from EF Core's perspective. CategoryController.Delete explicitly nulls the FK first.
/// </summary>
public class Category
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    // Any colour picked by the user as "#rrggbb"; null = automatic colour from the name
    public string? Color { get; set; }
}
