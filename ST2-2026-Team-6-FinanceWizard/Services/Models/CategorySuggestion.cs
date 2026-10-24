namespace ST2_2026_Team_6_FinanceWizard.Services.Models;

// Answer of IAiCategoryService.
//  CategoryId / CategoryName   - the user's existing category that fits (null when none fits)
//  SuggestedConcept            - what the text looks like ("Transport") even if the user has no such category yet
//  Confidence                  - 0..1
//  Source                      - "history" (the user categorised the same text before), "keyword" or "none"
public record CategorySuggestion(
    long? CategoryId,
    string? CategoryName,
    string? SuggestedConcept,
    double Confidence,
    string Source,
    string? Reason)
{
    public static CategorySuggestion None { get; } = new(null, null, null, 0, "none", null);
}
