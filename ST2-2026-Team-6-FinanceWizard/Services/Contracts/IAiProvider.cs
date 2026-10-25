namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

public interface IAiProvider
{
    string ProviderName { get; }
    Task<string> GetResponseAsync(string prompt, CancellationToken cancellationToken = default);
}