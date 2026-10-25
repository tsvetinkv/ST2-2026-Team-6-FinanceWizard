using Microsoft.Extensions.Caching.Memory;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.Text.Json;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    /// <summary>
    /// Design pattern: ADAPTER.
    /// Hides the external Frankfurter (ECB) HTTP API behind IExchangeRateService,
    /// so the rest of the app never deals with HTTP or JSON. Rates are cached for 1 hour.
    /// </summary>
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;

        public ExchangeRateService(HttpClient http, IMemoryCache cache)
        {
            _http = http;
            _cache = cache;
        }

        public async Task<decimal> GetRateAsync(string fromCode, string toCode, CancellationToken ct = default)
        {
            fromCode = fromCode.ToUpperInvariant();
            toCode = toCode.ToUpperInvariant();
            if (fromCode == toCode) return 1m;

            var fromPerEur = await GetPerEurAsync(fromCode, ct);
            var toPerEur = await GetPerEurAsync(toCode, ct);
            return toPerEur / fromPerEur;
        }

        // How many units of `code` equal 1 EUR
        private async Task<decimal> GetPerEurAsync(string code, CancellationToken ct)
        {
            if (code == "EUR") return 1m;

            return await _cache.GetOrCreateAsync($"fx:{code}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                try
                {
                    using var response = await _http.GetAsync($"v1/latest?base=EUR&symbols={code}", ct);
                    response.EnsureSuccessStatusCode();
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    return doc.RootElement.GetProperty("rates").GetProperty(code).GetDecimal();
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
                    or JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
                {
                    throw new ExchangeRateUnavailableException("Exchange rate unavailable.", ex);
                }
            });
        }
    }
}