using ST2_2026_Team_6_FinanceWizard.Services.Models;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

// Statistics for the dashboard charts and KPI cards
public interface IStatsService
{
    // Income vs. expenses per month for the last `months` months (including the current one),
    // spending by category over the same period and the KPIs of the current month.
    // Money of different currencies is never added up: all amounts are in ONE currency, either the
    // requested one or (when none is given) the currency with the most transactions in the period.
    Task<DashboardStats> GetStatsAsync(long userId, int months = 6, DateTime? today = null, string? currency = null);
}