using ST2_2026_Team_6_FinanceWizard.Services.Models;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

/// <summary>
/// Facade: one simple entry point for everything the dashboard shows.
///
/// Design pattern: FACADE.
/// - The dashboard needs data from several services (wallets, stats, transactions, merchants, categories, tags).
/// - Instead of the controller orchestrating all of them, it calls one method and gets a DashboardData.
/// - The controller stays thin and reusable (the same façade can power an API endpoint later).
/// </summary>
public interface IDashboardFacade
{
    /// <summary>
    /// Returns wallet balances, recent transactions, counters, KPIs and chart statistics
    /// for the given user.
    /// <paramref name="currency"/> picks which currency the KPIs and charts are shown in
    /// (null = the currency with the most transactions, as decided by StatsService).
    /// </summary>
    Task<DashboardData> GetDashboardDataAsync(long userId, string? currency = null);
}
