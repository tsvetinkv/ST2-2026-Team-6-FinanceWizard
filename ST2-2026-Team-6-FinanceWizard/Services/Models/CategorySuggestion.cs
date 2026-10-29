namespace ST2_2026_Team_6_FinanceWizard.Services.Models;

/// <summary>
/// Answer of IAiCategoryService.
///
/// Fields:
///   CategoryId / CategoryName   - the user's existing category that fits (null when none fits)
///   SuggestedConcept            - what the text looks like ("Transport") even if the user has no such category yet
///   Confidence                  - 0..1, how sure the service is (used by the UI to decide whether
///                                 to pre-select automatically or just suggest)
///   Source                      - "history" (the user categorised the same text before),
///                                 "keyword" (matched a rule), or "none"
///   Reason                      - optional human-readable explanation shown as a hint under the field
///
/// Design notes:
/// - A record: immutable, value-comparable, cheap to pass around.
/// - The static None is a shared "no suggestion" value, so callers can early-return it without
///   constructing a new object.
/// </summary>
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
