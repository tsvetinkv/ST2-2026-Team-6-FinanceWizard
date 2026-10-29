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
    /// <summary>
    /// CRUD for transactions plus the AI category-suggestion endpoint.
    ///
    /// Design patterns used:
    /// - MVC Controller + Repository pattern.
    /// - Builder pattern: TransactionQueryBuilder assembles the LINQ query step by step
    ///   (filter by user → date range → text → include related entities → order).
    ///   This keeps complex query composition readable and testable.
    /// - Strategy pattern: ITransactionService encapsulates the balance-update logic,
    ///   so the controller does not have to know how to mutate Wallet.Balance consistently.
    /// </summary>
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

        /// <summary>
        /// GET /Transactions — paginated and filterable list of the current user's transactions.
        ///
        /// The query is composed with TransactionQueryBuilder (Builder pattern):
        /// each chained call adds one condition to the underlying LINQ query.
        /// The query is translated to SQL by EF Core, so filtering and paging
        /// happen in the database — only the current page is loaded into memory.
        /// </summary>
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

            int totalItems = await query.CountAsync(); // another SQL round-trip, only a COUNT(*)
            filter.Transactions = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
            filter.CurrentPage = page;
            filter.TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            filter.Wallets = await _walletService.GetAllByUserIdAsync(userId);

            return View(filter);
        }

        /// <summary>
        /// GET /Transactions/Create — shows the transaction form, pre-selecting the spending wallet.
        /// </summary>
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

        /// <summary>
        /// POST /Transactions/Create — validates and persists a new transaction.
        ///
        /// All business rules (wallet ownership, sufficient balance, references exist)
        /// live in TransactionService, so the controller only has to catch
        /// InvalidOperationException and display the message.
        /// </summary>
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

        /// <summary>
        /// GET /Transactions/SuggestCategory — AI-assisted category suggestion.
        ///
        /// Called by JavaScript in the transaction form as the user types.
        /// The controller resolves the merchant name (the service works with names, not ids)
        /// and delegates the actual suggestion to IAiCategoryService.
        ///
        /// Response cache is disabled so each keystroke gets fresh analysis.
        /// </summary>
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
                    .Select(m => m.Name) // project only the name, not the whole entity
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var suggestion = await _aiCategoryService.SuggestAsync(GetUserId(), description, merchantName, cancellationToken);
            return Json(suggestion); // serialised to JSON for the client script
        }

        /// <summary>
        /// GET /Transactions/Edit/{id} — loads an existing transaction into the form.
        /// </summary>
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

        /// <summary>
        /// POST /Transactions/Edit/{id} — applies the edits.
        ///
        /// The balance-correction logic (remove original from old wallet, apply new amount
        /// to new wallet, refuse if the result would go negative) lives in TransactionService.
        /// </summary>
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

        /// <summary>
        /// POST /Transactions/Delete/{id} — deletes the transaction and reverses its balance effect.
        ///
        /// Accepts an optional returnUrl so the user is sent back to the same
        /// page and filters they were on. Only local URLs are allowed (open-redirect protection).
        /// </summary>
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

        /// <summary>
        /// Fills the dropdown / checkbox lists used by the transaction form.
        /// These are never posted back — they are decorated with [ValidateNever].
        /// </summary>
        private async Task PopulateOptionsAsync(TransactionCreateViewModel model)
        {
            model.Wallets = await _walletService.GetAllByUserIdAsync(GetUserId());
            model.Merchants = await _merchantRepo.All().AsNoTracking().OrderBy(m => m.Name).ToListAsync();
            model.Categories = await _categoryRepo.All().AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            model.Tags = await _tagRepo.All().AsNoTracking().OrderBy(t => t.Name).ToListAsync();
        }
    }
}
