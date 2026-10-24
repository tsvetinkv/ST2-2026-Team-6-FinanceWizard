using ST2_2026_Team_6_FinanceWizard.Services.Models;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

// Suggests a category for a transaction from its description and / or merchant name
public interface IAiCategoryService
{
    Task<CategorySuggestion> SuggestAsync(
        long userId,
        string? description,
        string? merchantName,
        CancellationToken cancellationToken = default);
}
