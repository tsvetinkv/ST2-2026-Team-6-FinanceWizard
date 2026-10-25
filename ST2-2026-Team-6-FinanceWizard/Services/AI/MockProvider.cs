using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services.AI;

public sealed class MockProvider : IAiProvider
{
    public string ProviderName => "Offline simulated provider (Mock)";

    public Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        string mockAdvice = "### [Simulated financial advice]\n\n" +
                            "1. **Expense analysis**: The majority of your expenses are for basic monthly needs.\n" +
                            "2. **Recommendation**: Set aside at least 15-20% of your current balance for an emergency fund.\n" +
                            "3. **Limits**: Set a monthly budget for categories with unexpected expenses.";

        return Task.FromResult(mockAdvice);
    }
}