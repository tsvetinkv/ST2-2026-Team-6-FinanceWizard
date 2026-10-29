namespace ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

/// <summary>
/// Tag = a free-form label attached to a transaction (many-to-many).
///
/// Design notes:
/// - Unlike Category, Color is REQUIRED here: a tag without a colour would not
///   be distinguishable from another tag at a glance.
/// - Icon is optional: null means "just text, no icon".
/// - Tag names are stored without the leading '#'; the TagController strips it on input.
/// </summary>
public class Tag
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Required colour as "#rrggbb"
    public string Color { get; set; } = "#1d4ed8";

    // Optional Bootstrap Icons name (e.g. "airplane"); null = no icon
    public string? Icon { get; set; }
}
