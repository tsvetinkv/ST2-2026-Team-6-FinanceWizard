using ST2_2026_Team_6_FinanceWizard.Services.Models;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

// Facade: one simple entry point for everything the dashboard shows
public interface IDashboardFacade
{
    // Wallet balances, recent transactions, counters, KPIs and chart statistics of the user
    // `currency` picks which currency the KPIs and charts are shown in (null = the most used one)
    Task<DashboardData> GetDashboardDataAsync(long userId, string? currency = null);
}