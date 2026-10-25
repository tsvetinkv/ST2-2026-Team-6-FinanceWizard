namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// The 8 supported currencies. Wallet.Currency stores the ISO Code (e.g. "EUR").
    /// </summary>
    public record CurrencyInfo(string Code, string Name)
    {
        public static readonly IReadOnlyList<CurrencyInfo> All = new[]
        {
            new CurrencyInfo("USD", "US dollar"),
            new CurrencyInfo("EUR", "Euro"),
            new CurrencyInfo("JPY", "Japanese yen"),
            new CurrencyInfo("GBP", "British pound"),
            new CurrencyInfo("AUD", "Australian dollar"),
            new CurrencyInfo("CAD", "Canadian dollar"),
            new CurrencyInfo("CHF", "Swiss franc"),
            new CurrencyInfo("CNY", "Chinese yuan")
        };

        // Old rows may still contain a symbol (¥ is ambiguous, so it is not mapped)
        private static readonly Dictionary<string, string> LegacySymbols = new()
        {
            ["€"] = "EUR",
            ["$"] = "USD",
            ["£"] = "GBP"
        };

        public static CurrencyInfo? Find(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return null;
            var value = stored.Trim();

            if (LegacySymbols.TryGetValue(value, out var mapped)) value = mapped;

            return All.FirstOrDefault(c => string.Equals(c.Code, value, StringComparison.OrdinalIgnoreCase));
        }
    }
}