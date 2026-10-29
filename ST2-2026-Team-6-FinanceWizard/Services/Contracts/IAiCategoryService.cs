using ST2_2026_Team_6_FinanceWizard.Services.Models;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

/// <summary>
/// Contract for "suggest a category for a transaction".
///
/// Design pattern: STRATEGY interface.
/// - The current implementation (AiCategoryService) uses rules + history.
/// - A future implementation could call an LLM. Swapping it is a one-line change in Program.cs,
///   because everything depends on this interface, not on the concrete class.
/// </summary>
public interface IAiCategoryService
{
    /// <summary>
    /// Suggests a category for the given description and / or merchant name.
    /// Returns <see cref="CategorySuggestion.None"/> when there is not enough information.
    /// </summary>
    Task<CategorySuggestion> SuggestAsync(
        long userId,
        string? description,
        string? merchantName,
        CancellationToken cancellationToken = default);
}
