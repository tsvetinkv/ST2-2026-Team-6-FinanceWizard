using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Services.Models;

// Everything the dashboard page needs, produced by IDashboardFacade in one call
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

// Headline numbers for the current calendar month (one currency, see StatsService)
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

// One bar group of the "Income vs. expenses" chart
public record MonthlyPoint(string Label, decimal Income, decimal Expense);

// One slice of the "Spending by category" chart. Color is "#rrggbb" or null (= automatic colour from Hue)
public record CategorySlice(string Name, decimal Total, string? Color, int Hue);

// Result of IStatsService: the chart + KPI numbers of one user
public class DashboardStats
{
    public string Currency { get; set; } = "EUR";
    public DashboardKpis Kpis { get; set; } = new();
    public List<MonthlyPoint> Monthly { get; set; } = new();
    public List<CategorySlice> SpendingByCategory { get; set; } = new();
    public List<string> AvailableCurrencies { get; set; } = new();
}