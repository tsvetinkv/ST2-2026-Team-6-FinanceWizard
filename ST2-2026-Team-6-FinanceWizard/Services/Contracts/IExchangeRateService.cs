namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts
{
    public interface IExchangeRateService
    {
        /// <summary>How many units of toCode you get for 1 unit of fromCode.</summary>
        Task<decimal> GetRateAsync(string fromCode, string toCode, CancellationToken ct = default);
    }

    public class ExchangeRateUnavailableException : Exception
    {
        public ExchangeRateUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
    }
}