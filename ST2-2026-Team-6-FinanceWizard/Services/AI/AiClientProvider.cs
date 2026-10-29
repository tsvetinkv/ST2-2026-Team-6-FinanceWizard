#nullable enable
namespace ST2_2026_Team_6_FinanceWizard.Services.AI;

/// <summary>
/// Singleton Pattern: Manages global AI model options and timeouts across the application.
/// Contains no user state and maintains zero dependencies on Scoped services.
///
/// Why Singleton here:
/// - The API key, model name and timeout are configuration values, identical for every request.
///   Creating them per request would be wasteful and (worse) risk two slightly different configurations.
/// - Being a Singleton means GeminiProvider can read them without injecting a Scoped service
///   (which is important because GeminiProvider is registered as a typed HttpClient,
///   which is itself a Singleton-like lifetime).
///
/// Initialisation happens once in Program.cs. The private constructor prevents accidental
/// instantiation from outside.
/// </summary>
public sealed class AiClientProvider
{
    private static readonly Lazy<AiClientProvider> _instance = new(() => new AiClientProvider());
    public static AiClientProvider Instance => _instance.Value;

    public string ApiKey { get; private set; } = string.Empty;
    public string Model { get; private set; } = "gemini-3.5-flash-lite";
    public TimeSpan Timeout { get; private set; } = TimeSpan.FromSeconds(30);

    private AiClientProvider() { }

    /// <summary>
    /// Initialises the API key, model and timeout. Called once at startup.
    /// Blank/null values are ignored so a missing config cannot accidentally wipe a previously set value.
    /// </summary>
    public void Initialize(string apiKey, string model = "gemini-3.5-flash-lite", int timeoutSeconds = 30)
    {
        ApiKey = apiKey ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(model)) Model = model;
        if (timeoutSeconds > 0) Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }
}
