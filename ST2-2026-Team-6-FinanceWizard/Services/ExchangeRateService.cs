using Microsoft.Extensions.Caching.Memory;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.Text.Json;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    /// <summary>
    /// Design pattern: ADAPTER.
    /// Hides the external Frankfurter (ECB) HTTP API behind IExchangeRateService,
    /// so the rest of the app never deals with HTTP or JSON. Rates are cached for 1 hour.
    ///
    /// Why adapter:
    /// - WalletService and AccountController just want "give me a decimal rate".
    /// - They should not have to know the URL, the JSON structure, or that a HttpClient is involved.
    /// - The adapter also handles errors uniformly: any failure (network, JSON, HTTP, missing key)
    ///   is converted to a single ExchangeRateUnavailableException, which callers already handle.
    ///
    /// Caching:
    /// - IMemoryCache stores "how many units of code = 1 EUR" for 1 hour,
    ///   which drastically reduces calls to the public API.
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

        /// <summary>
        /// Returns the rate such that: amount_in_from * rate = amount_in_to.
        /// Both legs are fetched against EUR, then divided — a common trick that avoids
        /// needing a separate endpoint for every pair.
        /// </summary>
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
                    // Translate every low-level failure into one domain-specific exception.
                    throw new ExchangeRateUnavailableException("Exchange rate unavailable.", ex);
                }
            });
        }
    }
}
