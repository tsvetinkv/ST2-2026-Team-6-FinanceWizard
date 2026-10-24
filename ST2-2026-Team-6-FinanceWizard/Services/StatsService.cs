using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using ST2_2026_Team_6_FinanceWizard.Services.Models;
using System.Globalization;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    public class StatsService : IStatsService
    {
        private const int MaxCategorySlices = 6;
        private const string UncategorizedName = "Uncategorized";
        private const string OtherName = "Other";

        private readonly IEfRepository<Transaction> _transactionRepo;
        private readonly IWalletService _walletService;

        public StatsService(IEfRepository<Transaction> transactionRepo, IWalletService walletService)
        {
            _transactionRepo = transactionRepo;
            _walletService = walletService;
        }

        public async Task<DashboardStats> GetStatsAsync(long userId, int months = 6, DateTime? today = null, string? currency = null)
        {
            months = Math.Clamp(months, 1, 24);
            var now = today ?? DateTime.Now;

            var firstMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-(months - 1));
            var endExclusive = new DateTime(now.Year, now.Month, 1).AddMonths(1);

            // All transactions of this user's wallets in the period (always read fresh from the database)
            var inPeriod = _transactionRepo.All()
                .AsNoTracking()
                .Where(t => t.Wallet.UserId == userId
                            && t.Date >= firstMonth
                            && t.Date < endExclusive);

            // Money of different currencies must not be added up, so the charts show ONE currency.
            // Which one: the requested currency, otherwise the one with the most transactions in the period.
            var activity = await inPeriod
                .GroupBy(t => t.Wallet.Currency)
                .Select(g => new { Currency = g.Key, Count = g.Count() })
                .ToListAsync();

            var wallets = await _walletService.GetAllByUserIdAsync(userId);

            var available = wallets.Select(w => w.Currency)
                .Concat(activity.Select(a => a.Currency))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToList();

            string selected =
                available.FirstOrDefault(c => string.Equals(c, currency?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? activity.OrderByDescending(a => a.Count).ThenBy(a => a.Currency, StringComparer.Ordinal)
                          .Select(a => a.Currency).FirstOrDefault()
                ?? wallets.GroupBy(w => w.Currency)
                          .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                          .Select(g => g.Key).FirstOrDefault()
                ?? "EUR";

            if (!available.Contains(selected, StringComparer.OrdinalIgnoreCase))
            {
                available.Add(selected);
            }

            // The category is loaded together with every transaction, so its real name and colour are used
            var rows = await inPeriod
                .Include(t => t.Category)
                .Where(t => t.Wallet.Currency == selected)
                .ToListAsync();

            // ---- Income vs. expenses per month ----
            var monthly = new List<MonthlyPoint>();
            for (int i = 0; i < months; i++)
            {
                var start = firstMonth.AddMonths(i);
                var end = start.AddMonths(1);
                var inMonth = rows.Where(r => r.Date >= start && r.Date < end).ToList();

                decimal income = inMonth.Where(IsIncome).Sum(r => r.Amount);
                decimal expense = inMonth.Where(r => !IsIncome(r)).Sum(r => r.Amount);

                // "Oct" for normal months, "Jan 27" when the year changes so the axis stays readable
                string label = start.Month == 1 || i == 0
                    ? start.ToString("MMM yy", CultureInfo.InvariantCulture)
                    : start.ToString("MMM", CultureInfo.InvariantCulture);

                monthly.Add(new MonthlyPoint(label, income, expense));
            }

            // ---- KPIs of the current month ----
            var current = monthly[^1];
            decimal? previousExpense = months > 1 ? monthly[^2].Expense : null;

            var kpis = new DashboardKpis
            {
                Currency = selected,
                IncomeThisMonth = current.Income,
                ExpensesThisMonth = current.Expense,
                SavingsRatePercent = current.Income > 0
                    ? Math.Round((current.Income - current.Expense) / current.Income * 100m, 0)
                    : null,
                ExpenseChangePercent = previousExpense.HasValue && previousExpense.Value > 0
                    ? Math.Round((current.Expense - previousExpense.Value) / previousExpense.Value * 100m, 0)
                    : null
            };

            // ---- Spending by category over the whole period ----
            var expenseGroups = rows
                .Where(r => !IsIncome(r))
                .GroupBy(r => r.Category?.Name ?? UncategorizedName)
                .Select(g => new
                {
                    Name = g.Key,
                    Total = g.Sum(r => r.Amount),
                    Color = g.Select(r => r.Category?.Color).FirstOrDefault(c => c != null)
                })
                .OrderByDescending(g => g.Total)
                .ToList();

            var slices = expenseGroups
                .Take(MaxCategorySlices)
                .Select(g => new CategorySlice(g.Name, g.Total, Colors.Normalize(g.Color), Hue.Of(g.Name)))
                .ToList();

            decimal rest = expenseGroups.Skip(MaxCategorySlices).Sum(g => g.Total);
            if (rest > 0)
            {
                slices.Add(new CategorySlice(OtherName, rest, null, Hue.Of(OtherName)));
            }

            return new DashboardStats
            {
                Currency = selected,
                Kpis = kpis,
                Monthly = monthly,
                SpendingByCategory = slices,
                AvailableCurrencies = available
            };
        }

        private static bool IsIncome(Transaction t) =>
            string.Equals(t.Type, "Income", StringComparison.OrdinalIgnoreCase);
    }
}