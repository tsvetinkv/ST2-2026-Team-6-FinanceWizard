namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts
{
    /// <summary>
    /// Contract for a currency-rate provider.
    ///
    /// Design pattern: ADAPTER interface.
    /// - The concrete ExchangeRateService adapts the Frankfurter HTTP API to this interface.
    /// - Consumers (WalletService) only see "give me a decimal rate" — no HTTP, no JSON, no URL.
    /// - A different implementation (a paid API, a fixed-rate table) could be swapped in
    ///   without touching WalletService.
    /// </summary>
    public interface IExchangeRateService
    {
        /// <summary>How many units of toCode you get for 1 unit of fromCode.</summary>
        Task<decimal> GetRateAsync(string fromCode, string toCode, CancellationToken ct = default);
    }

    /// <summary>
    /// Domain-specific exception thrown when the rate cannot be fetched
    /// (network error, malformed JSON, missing currency, ...).
    ///
    /// Why a dedicated exception:
    /// - Callers (WalletService) can catch this exact type and translate it into a user-friendly
    ///   message, without knowing which low-level exception occurred.
    /// - The original exception is preserved in InnerException for logging.
    /// </summary>
    public class ExchangeRateUnavailableException : Exception
    {
        public ExchangeRateUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
    }
}
