using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Models;

namespace ST2_2026_Team_6_FinanceWizard.Models
{
    /// <summary>
    /// View-model for the dashboard.
    ///
    /// Design notes:
    /// - This is the UI-facing shape of the data. The service returns a DashboardData DTO,
    ///   and the static FromDashboardData method maps it. This separates "what the service needs"
    ///   from "what the view needs" (a common reason to keep them apart: the view may later need
    ///   pre-computed flags like HasChartData, while the service should not know about views).
    /// </summary>
    public class HomeDashboardViewModel
    {
        public List<Wallet> Wallets { get; set; } = new();
        public List<Transaction> RecentTransactions { get; set; } = new();

        public int TransactionCount { get; set; }
        public int MerchantCount { get; set; }
        public int CategoryCount { get; set; }
        public int TagCount { get; set; }

        // KPI cards and chart data (one currency, see StatsService)
        public DashboardKpis Kpis { get; set; } = new();
        public List<MonthlyPoint> Monthly { get; set; } = new();
        public List<CategorySlice> SpendingByCategory { get; set; } = new();

        // Currencies the user can switch between (the KPIs and charts show one at a time)
        public List<string> AvailableCurrencies { get; set; } = new();

        // True when there is anything to draw in the charts
        public bool HasChartData => Monthly.Any(m => m.Income > 0 || m.Expense > 0);

        /// <summary>
        /// Maps a DashboardData DTO to this view-model.
        /// Static factory method pattern: the mapping lives next to the view-model,
        /// not in the controller, and returns a fully-initialised object.
        /// </summary>
        public static HomeDashboardViewModel FromDashboardData(DashboardData data) => new()
        {
            Wallets = data.Wallets,
            RecentTransactions = data.RecentTransactions,
            TransactionCount = data.TransactionCount,
            MerchantCount = data.MerchantCount,
            CategoryCount = data.CategoryCount,
            TagCount = data.TagCount,
            Kpis = data.Kpis,
            Monthly = data.Monthly,
            SpendingByCategory = data.SpendingByCategory,
            AvailableCurrencies = data.AvailableCurrencies
        };
    }
}
