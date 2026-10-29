using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Models;

/// <summary>
/// Everything the dashboard page needs, produced by IDashboardFacade in one call.
///
/// Design notes:
/// - This DTO is a "data bag" with no behaviour. Keeping it separate from the view-model
///   (HomeDashboardViewModel) means the service layer does not need to know about Razor,
///   formatting helpers or pre-computed flags like HasChartData.
/// - The DTO is filled by one method that guarantees the numbers are consistent (same user, same period,
///   same currency), which would be easy to break if the controller fetched each piece itself.
/// </summary>
public class DashboardData
{
    public List<Wallet> Wallets { get; set; } = new();
    public List<Transaction> RecentTransactions { get; set; } = new();

    public int TransactionCount { get; set; }
    public int MerchantCount { get; set; }
    public int CategoryCount { get; set; }
    public int TagCount { get; set; }

    public DashboardKpis Kpis { get; set; } = new();
    public List<MonthlyPoint> Monthly { get; set; } = new();
    public List<CategorySlice> SpendingByCategory { get; set; } = new();

    // Every currency the user has a wallet or a recent transaction in (for the currency switch)
    public List<string> AvailableCurrencies { get; set; } = new();
}

/// <summary>
/// Headline numbers for the current calendar month (one currency, see StatsService).
///
/// Design notes:
/// - Nullable fields express "not applicable":
///   • SavingsRatePercent is null when there was no income (percentage of zero is meaningless).
///   • ExpenseChangePercent is null when last month had no expenses (percentage change from zero is meaningless).
/// - NetThisMonth is a computed property: it derives from the two amounts, so it can never drift.
/// </summary>
public class DashboardKpis
{
    public string Currency { get; set; } = "EUR";
    public decimal IncomeThisMonth { get; set; }
    public decimal ExpensesThisMonth { get; set; }
    public decimal NetThisMonth => IncomeThisMonth - ExpensesThisMonth;

    // (income - expenses) / income * 100; null when there was no income this month
    public decimal? SavingsRatePercent { get; set; }

    // Expenses compared with last month in percent; null when last month had no expenses
    public decimal? ExpenseChangePercent { get; set; }
}

/// <summary>
/// One bar group of the "Income vs. expenses" chart. The Label is pre-formatted ("Oct" / "Jan 27")
/// so the chart script does not have to worry about month formatting.
/// </summary>
public record MonthlyPoint(string Label, decimal Income, decimal Expense);

/// <summary>
/// One slice of the "Spending by category" chart. Color is "#rrggbb" or null (= automatic colour from Hue).
/// Hue is a stable 0-359 value derived from the name; the chart uses it when Color is null,
/// so each category gets a consistent colour even without a manual choice.
/// </summary>
public record CategorySlice(string Name, decimal Total, string? Color, int Hue);

/// <summary>
/// Result of IStatsService: the chart + KPI numbers of one user.
/// The Currency field explains which currency all the numbers are in; the UI must show that.
/// </summary>
public class DashboardStats
{
    public string Currency { get; set; } = "EUR";
    public DashboardKpis Kpis { get; set; } = new();
    public List<MonthlyPoint> Monthly { get; set; } = new();
    public List<CategorySlice> SpendingByCategory { get; set; } = new();
    public List<string> AvailableCurrencies { get; set; } = new();
}
