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
        private const int PageSize = 10;

        private readonly IEfRepository<Transaction> _transactionRepo;
        private readonly ITransactionService _transactionService;
        private readonly IWalletService _walletService;

        public TransactionsController(
            IEfRepository<Transaction> transactionRepo,
            ITransactionService transactionService,
            IWalletService walletService)
        {
            _transactionRepo = transactionRepo;
            _transactionService = transactionService;
            _walletService = walletService;
        }

        private long GetUserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> Index(TransactionListViewModel filter, int page = 1)
        {
            long userId = GetUserId();
            if (page < 1) page = 1;

            // Filter automatically by the user's single wallet
            var query = new TransactionQueryBuilder(_transactionRepo.All())
                .FilterByUser(userId)
                .FilterByDateRange(filter.FromDate, filter.ToDate)
                .SearchText(filter.SearchTerm)
                .OrderByDescendingDate()
                .Build();

            int totalItems = query.Count();
            filter.Transactions = query.Skip((page - 1) * PageSize).Take(PageSize).ToList();
            filter.CurrentPage = page;
            filter.TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            // Load the user's wallet
            filter.Wallet = await _walletService.GetByUserIdAsync(userId);

            return View(filter);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new TransactionCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _transactionService.CreateTransactionAsync(
                    model.Amount,
                    model.Description ?? string.Empty,
                    model.Type,
                    model.Date != default ? model.Date : DateTime.Now,
                    GetUserId());

                TempData["SuccessMessage"] = "Transaction saved successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                // Insufficient funds / wallet not found
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id, string? returnUrl)
        {
            try
            {
                await _transactionService.DeleteTransactionAsync(id, GetUserId());
                TempData["SuccessMessage"] = "Transaction deleted and wallet balance updated.";
            }
            catch (InvalidOperationException ex)
            {
                // Transaction not found / balance would become negative
                TempData["ErrorMessage"] = ex.Message;
            }

            // Return to the same page and filters the user was on (local URLs only)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(TransactionCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid deposit amount.";
                return RedirectToAction(nameof(Index));
            }

            var userId = GetUserId();
            var wallet = await _walletService.GetByUserIdAsync(userId);

            if (wallet != null)
            {
                await _walletService.UpdateBalanceAsync(userId, wallet.Balance + model.Amount);
                TempData["SuccessMessage"] = $"Successfully added {model.Amount:F2} {wallet.Currency}.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
