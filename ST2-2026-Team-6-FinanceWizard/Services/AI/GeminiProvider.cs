#nullable enable
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Services.AI;

public class GeminiProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<GeminiProvider> _logger;

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

    public async Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var apiKey = AiClientProvider.Instance.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "Gemini API key is missing. Please configure 'Gemini:ApiKey' using dotnet user-secrets.";
        }

        var model = AiClientProvider.Instance.Model;
        if (model.StartsWith("models/", StringComparison.Ordinal))
        {
            model = model[7..];
        }

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
                _logger.LogError("Gemini API error (HTTP {StatusCode}): {Body}", (int)response.StatusCode, body);
                return $"[Gemini API Error HTTP {(int)response.StatusCode}]: {body}";
            }

            var result = JsonSerializer.Deserialize<GeminiResponse>(body, _jsonOptions);
            var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            return string.IsNullOrWhiteSpace(text) ? "No response received from the AI model." : text.Trim();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Network or timeout error calling Gemini API.");
            return "Request timed out or a network error occurred. Please check your connection.";
        }
    }

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