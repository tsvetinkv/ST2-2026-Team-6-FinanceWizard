#nullable enable
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services.AI;

public class MockProvider : IAiProvider
{
    public string ProviderName => "Offline Mock Advisor";

    public Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var input = prompt.ToLowerInvariant();
        string response = input switch
        {
            var p when p.Contains("summarize") || p.Contains("spending") || p.Contains("разходи") =>
                "Based on your recent transaction history, your top spending categories are Housing, Dining, and Groceries. Consider setting tighter category limits.",
            var p when p.Contains("save") || p.Contains("saving") || p.Contains("спестя") =>
                "To increase savings: 1) Allocate 15% to savings immediately after receiving income, 2) Audit monthly subscriptions, 3) Review recurring merchant charges.",
            _ => "Your financial profile looks stable. Keep recording daily transactions and maintaining active budget limits."
        };

        return Task.FromResult($"[Offline Mode] {response}");
    }
}