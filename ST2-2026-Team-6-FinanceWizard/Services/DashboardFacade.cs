using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using ST2_2026_Team_6_FinanceWizard.Services.Models;

namespace ST2_2026_Team_6_FinanceWizard.Services
{
    /// <summary>
    /// Facade pattern: hides the wallet service, the statistics service and four repositories
    /// behind one method, so controllers only depend on IDashboardFacade.
    ///
    /// Design pattern: FACADE.
    /// - Purpose: the dashboard needs data from several sub-systems. Instead of the controller
    ///   orchestrating five calls (and knowing about their order and return types), it asks one
    ///   façade for a single DashboardData object.
    /// - Benefits: the controller stays thin, the dashboard logic is reusable from other
    ///   entry points (e.g. an API endpoint) without duplication, and the sub-systems can be
    ///   refactored without changing controllers.
    /// </summary>
    public class DashboardFacade : IDashboardFacade
    {
        private const int RecentTransactionCount = 5;
        private const int ChartMonths = 6;

        private readonly IWalletService _walletService;
        private readonly IStatsService _statsService;
        private readonly IEfRepository<Transaction> _transactionRepo;
        private readonly IEfRepository<Merchant> _merchantRepo;
        private readonly IEfRepository<Category> _categoryRepo;
        private readonly IEfRepository<Tag> _tagRepo;

        public DashboardFacade(
            IWalletService walletService,
            IStatsService statsService,
            IEfRepository<Transaction> transactionRepo,
            IEfRepository<Merchant> merchantRepo,
            IEfRepository<Category> categoryRepo,
            IEfRepository<Tag> tagRepo)
        {
            _walletService = walletService;
            _statsService = statsService;
            _transactionRepo = transactionRepo;
            _merchantRepo = merchantRepo;
            _categoryRepo = categoryRepo;
            _tagRepo = tagRepo;
        }

        /// <summary>
        /// Builds everything the dashboard needs in one call:
        /// wallets, counts, recent transactions and stats (KPIs + monthly + category slices).
        /// </summary>
        public async Task<DashboardData> GetDashboardDataAsync(long userId, string? currency = null)
        {
            // One DbContext is shared by all services of a request, so the calls run one after another
            var userTransactions = _transactionRepo.All().Where(t => t.Wallet.UserId == userId);

            var data = new DashboardData
            {
                Wallets = await _walletService.GetAllByUserIdAsync(userId),
                TransactionCount = await userTransactions.CountAsync(),
                MerchantCount = await _merchantRepo.All().CountAsync(),
                CategoryCount = await _categoryRepo.All().CountAsync(),
                TagCount = await _tagRepo.All().CountAsync(),
                RecentTransactions = await userTransactions
                    .AsNoTracking()
                    .Include(t => t.Wallet)
                    .Include(t => t.Merchant)
                    .Include(t => t.Category)
                    .OrderByDescending(t => t.Date)
                    .Take(RecentTransactionCount)
                    .ToListAsync()
            };

            // Always calculated from the database on every request - nothing is cached
            var stats = await _statsService.GetStatsAsync(userId, ChartMonths, null, currency);
            data.Kpis = stats.Kpis;
            data.Monthly = stats.Monthly;
            data.SpendingByCategory = stats.SpendingByCategory;
            data.AvailableCurrencies = stats.AvailableCurrencies;

            return data;
        }
    }
}
