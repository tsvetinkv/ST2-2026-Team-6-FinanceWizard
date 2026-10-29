#nullable enable
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services.AI;

/// <summary>
/// Offline AI provider used when no API key is configured.
///
/// Design pattern: STRATEGY (concrete strategy).
/// - Implements the same IAiProvider interface as GeminiProvider.
/// - Program.cs selects it at startup based on configuration
///   (no key → MockProvider, key present → GeminiProvider).
/// - The rest of the app never knows which provider is active.
///
/// Behaviour: pattern-matches a few keywords in the prompt and returns a canned reply,
/// so the whole feature is demonstrable without external services or credentials.
/// </summary>
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

        // The [Offline Mode] tag makes it obvious in the UI that this is not a real LLM reply.
        return Task.FromResult($"[Offline Mode] {response}");
    }
}
