namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// Value object (C# record) that describes one supported currency.
    ///
    /// Design notes:
    /// - The record type makes it immutable and gives value equality for free.
    /// - The static list is the single source of truth for "supported currencies".
    /// - LegacySymbols handles old data rows that stored a symbol (€, $, £) instead of the ISO code.
    ///   "¥" is deliberately excluded because it's ambiguous (JPY vs CNY).
    ///
    /// Currency.Currency stores the ISO code (e.g. "EUR"). CurrencyInfo.Find normalises any input
    /// (symbol or code) into the canonical CurrencyInfo record.
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

        /// <summary>
        /// Looks up a supported currency by code (case-insensitive) or by legacy symbol.
        /// Returns null when the value is empty or unsupported.
        /// </summary>
        public static CurrencyInfo? Find(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return null;
            var value = stored.Trim();

            // Symbol → ISO code mapping (backward compatibility).
            if (LegacySymbols.TryGetValue(value, out var mapped)) value = mapped;

            // Case-insensitive lookup in the list of supported currencies.
            return All.FirstOrDefault(c => string.Equals(c.Code, value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
