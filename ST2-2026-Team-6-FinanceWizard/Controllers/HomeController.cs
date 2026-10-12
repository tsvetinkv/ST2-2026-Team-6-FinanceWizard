using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.Diagnostics;
using System.Security.Claims;

namespace ST2_2026_Team_6_FinanceWizard.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWalletService _walletService;
        private readonly IEfRepository<Transaction> _transactionRepo;
        private readonly IEfRepository<Merchant> _merchantRepo;
        private readonly IEfRepository<Category> _categoryRepo;
        private readonly IEfRepository<Tag> _tagRepo;

        public HomeController(
            ILogger<HomeController> logger,
            IWalletService walletService,
            IEfRepository<Transaction> transactionRepo,
            IEfRepository<Merchant> merchantRepo,
            IEfRepository<Category> categoryRepo,
            IEfRepository<Tag> tagRepo)
        {
            _logger = logger;
            _walletService = walletService;
            _transactionRepo = transactionRepo;
            _merchantRepo = merchantRepo;
            _categoryRepo = categoryRepo;
            _tagRepo = tagRepo;
        }

        public async Task<IActionResult> Index()
        {
            // Guests see the landing page
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return View();
            }

            // Signed-in users see their dashboard with shortcuts to every section
            long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var userTransactions = _transactionRepo.All().Where(t => t.Wallet.UserId == userId);

            var model = new HomeDashboardViewModel
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
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
