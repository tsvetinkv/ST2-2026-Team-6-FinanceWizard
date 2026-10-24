using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
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
        private readonly IEfRepository<Merchant> _merchantRepo;
        private readonly IEfRepository<Category> _categoryRepo;
        private readonly IEfRepository<Tag> _tagRepo;
        private readonly ITransactionService _transactionService;
        private readonly IWalletService _walletService;
        private readonly IAiCategoryService _aiCategoryService;

        public TransactionsController(
            IEfRepository<Transaction> transactionRepo,
            IEfRepository<Merchant> merchantRepo,
            IEfRepository<Category> categoryRepo,
            IEfRepository<Tag> tagRepo,
            ITransactionService transactionService,
            IWalletService walletService,
            IAiCategoryService aiCategoryService)
        {
            _transactionRepo = transactionRepo;
            _merchantRepo = merchantRepo;
            _categoryRepo = categoryRepo;
            _tagRepo = tagRepo;
            _transactionService = transactionService;
            _walletService = walletService;
            _aiCategoryService = aiCategoryService;
        }

        private long GetUserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> Index(TransactionListViewModel filter, int page = 1)
        {
            long userId = GetUserId();
            if (page < 1) page = 1;

            // Only the transactions of the user's wallets
            var query = new TransactionQueryBuilder(_transactionRepo.All())
                .FilterByUser(userId)
                .FilterByDateRange(filter.FromDate, filter.ToDate)
                .SearchText(filter.SearchTerm)
                .IncludeDetails()
                .OrderByDescendingDate()
                .Build();

            int totalItems = await query.CountAsync();
            filter.Transactions = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
            filter.CurrentPage = page;
            filter.TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            filter.Wallets = await _walletService.GetAllByUserIdAsync(userId);

            return View(filter);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new TransactionCreateViewModel();
            await PopulateOptionsAsync(model);

            // Pre-select the spending wallet (or the first one)
            var preferred = model.Wallets.FirstOrDefault(w => w.Type == WalletTypes.Spending) ?? model.Wallets.FirstOrDefault();
            model.WalletId = preferred?.Id;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TransactionCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateOptionsAsync(model);
                return View(model);
            }

            try
            {
                await _transactionService.CreateTransactionAsync(
                    GetUserId(),
                    model.WalletId!.Value,
                    model.Amount,
                    model.Description ?? string.Empty,
                    model.Type,
                    model.Date != default ? model.Date : DateTime.Now,
                    model.MerchantId,
                    model.CategoryId,
                    model.TagIds ?? new List<long>());

                TempData["SuccessMessage"] = "Transaction saved successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                // Insufficient funds / wallet, merchant, category or tag not found
                ModelState.AddModelError("", ex.Message);
                await PopulateOptionsAsync(model);
                return View(model);
            }
        }

        // GET /Transactions/SuggestCategory?description=Starbucks&merchantId=3
        // Used by the transaction form to pre-select a category while the user types.
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> SuggestCategory(string? description, long? merchantId, CancellationToken cancellationToken)
        {
            string? merchantName = null;
            if (merchantId.HasValue)
            {
                merchantName = await _merchantRepo.All()
                    .AsNoTracking()
                    .Where(m => m.Id == merchantId.Value)
                    .Select(m => m.Name)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var suggestion = await _aiCategoryService.SuggestAsync(GetUserId(), description, merchantName, cancellationToken);
            return Json(suggestion);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var transaction = await _transactionService.GetByIdAsync(id, GetUserId());
            if (transaction == null) return NotFound();

            var model = new TransactionCreateViewModel
            {
                Type = transaction.Type,
                Amount = transaction.Amount,
                Date = transaction.Date,
                Description = transaction.Description,
                WalletId = transaction.WalletId,
                MerchantId = transaction.MerchantId,
                CategoryId = transaction.CategoryId,
                TagIds = transaction.TransactionTags.Select(tt => tt.TagId).ToList()
            };

            await PopulateOptionsAsync(model);
            ViewData["TransactionId"] = id;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, TransactionCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateOptionsAsync(model);
                ViewData["TransactionId"] = id;
                return View(model);
            }

            try
            {
                await _transactionService.UpdateTransactionAsync(
                    id,
                    GetUserId(),
                    model.WalletId!.Value,
                    model.Amount,
                    model.Description ?? string.Empty,
                    model.Type,
                    model.Date != default ? model.Date : DateTime.Now,
                    model.MerchantId,
                    model.CategoryId,
                    model.TagIds ?? new List<long>());

                TempData["SuccessMessage"] = "Transaction updated and wallet balances adjusted.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                // Transaction not found, insufficient funds, or a missing wallet / merchant / category / tag
                ModelState.AddModelError("", ex.Message);
                await PopulateOptionsAsync(model);
                ViewData["TransactionId"] = id;
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

        // Loads the wallets / merchants / categories / tags shown in the form
        private async Task PopulateOptionsAsync(TransactionCreateViewModel model)
        {
            model.Wallets = await _walletService.GetAllByUserIdAsync(GetUserId());
            model.Merchants = await _merchantRepo.All().AsNoTracking().OrderBy(m => m.Name).ToListAsync();
            model.Categories = await _categoryRepo.All().AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            model.Tags = await _tagRepo.All().AsNoTracking().OrderBy(t => t.Name).ToListAsync();
        }
    }
}
