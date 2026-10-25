#nullable enable
namespace ST2_2026_Team_6_FinanceWizard.Services.AI;

/// <summary>
/// Singleton Pattern: Manages global AI model options and timeouts across the application.
/// Contains no user state and maintains zero dependencies on Scoped services.
/// </summary>
public sealed class AiClientProvider
{
    private static readonly Lazy<AiClientProvider> _instance = new(() => new AiClientProvider());
    public static AiClientProvider Instance => _instance.Value;

    public string ApiKey { get; private set; } = string.Empty;
    public string Model { get; private set; } = "gemini-3.5-flash-lite";
    public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(30);

    private AiClientProvider() { }

    public void Initialize(string apiKey, string model = "gemini-3.5-flash-lite", int timeoutSeconds = 30)
    {
        ApiKey = apiKey ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(model)) Model = model;
        if (timeoutSeconds > 0) Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }
}