namespace ST2_2026_Team_6_FinanceWizard.Services.AI;

/// <summary>
/// Thread-safe Singleton for AI client configuration.
/// Does NOT contain user state and DOES NOT depend on Scoped services (DbContext).
/// </summary>
public sealed class AiClientProvider
{
    private static readonly Lazy<AiClientProvider> _instance = new(() => new AiClientProvider());
    public static AiClientProvider Instance => _instance.Value;

    public string ApiKey { get; private set; } = string.Empty;
    public string Model { get; private set; } = "gemini-3.5-flash";
    public int TimeoutSeconds { get; private set; } = 15;

    private AiClientProvider() { }

    public void Initialize(string apiKey, string model = "gemini-3.5-flash", int timeoutSeconds = 15)
    {
        ApiKey = apiKey ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(model)) Model = model;
        TimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 15;
    }
}