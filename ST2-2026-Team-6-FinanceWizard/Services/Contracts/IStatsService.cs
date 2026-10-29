using ST2_2026_Team_6_FinanceWizard.Services.Models;

namespace ST2_2026_Team_6_FinanceWizard.Services.Contracts;

/// <summary>
/// Contract for statistics that back the dashboard charts and KPI cards.
///
/// Design notes:
/// - One method returns KPIs + monthly series + category breakdown + available currencies
///   in a single DashboardStats object. This guarantees they are computed from the SAME
///   currency and period, so the UI cannot accidentally show mismatched numbers.
/// - Money of different currencies is never summed: the implementation picks one currency
///   (requested, or the most active one) and filters on it.
/// </summary>
public interface IStatsService
{
    /// <summary>
    /// Income vs. expenses per month for the last <paramref name="months"/> months
    /// (including the current one), spending by category over the same period,
    /// and the KPIs of the current month.
    ///
    /// <paramref name="today"/> lets tests inject a fixed "now" without touching the DB.
    /// <paramref name="currency"/> forces a specific display currency; null = pick the most active.
    /// </summary>
    Task<DashboardStats> GetStatsAsync(long userId, int months = 6, DateTime? today = null, string? currency = null);
}
