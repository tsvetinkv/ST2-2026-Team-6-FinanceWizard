#nullable enable
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ST2_2026_Team_6_FinanceWizard.Services.AI;

/// <summary>
/// Real AI provider: calls the Google Gemini REST API.
///
/// Design patterns used:
/// - ADAPTER: this class adapts the Gemini HTTP/JSON API to the app's IAiProvider interface.
///   The rest of the app only sees GetResponseAsync(string prompt) → string reply,
///   and knows nothing about HTTP, JSON, endpoints or API keys.
/// - STRATEGY (concrete strategy): this is one of the interchangeable IAiProvider implementations;
///   Program.cs selects it at startup based on configuration. MockProvider is the other one.
///
/// Notes:
/// - The API key is read from AiClientProvider.Instance (a singleton), so it is configured once.
/// - Any network / timeout error is returned as a user-friendly string rather than thrown,
///   because the assistant UI is expected to render whatever the provider returns.
/// </summary>
public class GeminiProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<GeminiProvider> _logger;

    // Serialiser settings shared by every request: camelCase + omit nulls.
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string ProviderName => "Google Gemini REST API";

    public GeminiProvider(HttpClient http, ILogger<GeminiProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Sends the prompt to Gemini and returns the model's reply text.
    /// </summary>
    public async Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var apiKey = AiClientProvider.Instance.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Fail gracefully: the assistant UI can show this message instead of crashing.
            return "Gemini API key is missing. Please configure 'Gemini:ApiKey' using dotnet user-secrets.";
        }

        // The Gemini REST path uses just the model id, so strip a leading "models/" if present.
        var model = AiClientProvider.Instance.Model;
        if (model.StartsWith("models/", StringComparison.Ordinal))
        {
            model = model[7..];
        }

        // Minimal Gemini payload: one content part with the prompt text.
        var payload = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{model}:generateContent")
        {
            Content = JsonContent.Create(payload, options: _jsonOptions)
        };
        request.Headers.Add("x-goog-api-key", apiKey);

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Log the full error for debugging, return a compact message for the UI.
                _logger.LogError("Gemini API error (HTTP {StatusCode}): {Body}", (int)response.StatusCode, body);
                return $"[Gemini API Error HTTP {(int)response.StatusCode}]: {body}";
            }

            // Gemini returns a list of candidates; we use the first one's first text part.
            var result = JsonSerializer.Deserialize<GeminiResponse>(body, _jsonOptions);
            var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            return string.IsNullOrWhiteSpace(text) ? "No response received from the AI model." : text.Trim();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Network failure or timeout — return a message instead of throwing.
            _logger.LogError(ex, "Network or timeout error calling Gemini API.");
            return "Request timed out or a network error occurred. Please check your connection.";
        }
    }

    // ---- Minimal DTOs for the Gemini response shape. ----
    // Only the fields we actually use are declared; unknown fields are ignored by System.Text.Json.

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
