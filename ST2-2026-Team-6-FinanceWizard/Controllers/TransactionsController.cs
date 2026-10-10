using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Services.Builders;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.Security.Claims;

namespace ST2_2026_Team_6_FinanceWizard.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly IEfRepository<Transaction> _transactionRepo;
        private readonly IWalletService _walletService;

        public TransactionsController(
            IEfRepository<Transaction> transactionRepo,
            IWalletService walletService)
        {
            _transactionRepo = transactionRepo;
            _walletService = walletService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(TransactionListViewModel filter, int page = 1)
        {
            long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            int pageSize = 10;

            var baseQuery = _transactionRepo.All();

            // Автоматично филтриране по единичния портфейл на потребителя
            var query = new TransactionQueryBuilder(baseQuery)
                .FilterByUser(userId)
                .FilterByDateRange(filter.FromDate, filter.ToDate)
                .SearchText(filter.SearchTerm)
                .OrderByDescendingDate()
                .Build();

            int totalItems = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            filter.Transactions = items;
            filter.CurrentPage = page;
            filter.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Зареждане на единствения портфейл на потребителя
            filter.Wallet = await _walletService.GetByUserIdAsync(userId);

            return View(filter);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new TransactionCreateViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionCreateViewModel model)
        {
            long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Взимане на единствения портфейл на текущия потребител
            var wallet = await _walletService.GetByUserIdAsync(userId);
            if (wallet == null)
            {
                ModelState.AddModelError("", "Портфейлът не беше намерен.");
                return View(model);
            }

            // 2. Проверка за наличност при разход
            if (model.Type == "Expense" && wallet.Balance < model.Amount)
            {
                ModelState.AddModelError("Amount", $"Нямате достатъчно средства! Наличност: {wallet.Balance:F2} {wallet.Currency}");
                return View(model);
            }

            // 3. Автоматично задаване на WalletId
            var transaction = new Transaction
            {
                Amount = model.Amount,
                Description = model.Description,
                Type = model.Type,
                WalletId = wallet.Id,
                Date = model.Date != default ? model.Date : DateTime.Now
            };

            await _transactionRepo.AddAsync(transaction);
            await _transactionRepo.SaveChangesAsync();

            // 4. Обновяване на баланса
            decimal newBalance = model.Type == "Income"
                ? wallet.Balance + model.Amount
                : wallet.Balance - model.Amount;

            await _walletService.UpdateBalanceAsync(userId, newBalance);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Deposit(TransactionCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var wallet = await _walletService.GetByUserIdAsync(userId);

            if (wallet != null)
            {
                decimal newBalance = wallet.Balance + model.Amount;
                await _walletService.UpdateBalanceAsync(userId, newBalance);
                TempData["SuccessMessage"] = $"Успешно добавихте {model.Amount:F2} {wallet.Currency}!";
            }

            return RedirectToAction("Index", "Transactions");
        }
    }
}