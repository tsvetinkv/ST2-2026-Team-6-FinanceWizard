#nullable enable
namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

/// <summary>
/// Contract for an AI chat provider (Gemini in production, MockProvider offline).
///
/// Design pattern: STRATEGY interface.
/// - Program.cs decides at startup which concrete implementation to register.
/// - Consumers (AssistantController) only see this interface, so they do not know or care
///   whether the reply comes from a real LLM or the offline mock.
/// - ProviderName is exposed so the UI can show which engine is active.
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// A short human-readable name ("Google Gemini REST API", "Offline Mock Advisor")
    /// shown in the assistant UI badge.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Sends the prompt and returns the reply text.
    /// Implementations are expected to handle network errors gracefully (return a message rather than throw),
    /// because the assistant UI renders whatever string comes back.
    /// </summary>
    Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default);
}
