using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.Globalization;
using System.Security.Claims;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

/// <summary>
/// CRUD for wallets (spending / savings accounts).
///
/// Design patterns used:
/// - MVC Controller.
/// - IWalletService abstracts the balance-conversion logic (currency change) away from the controller.
///   The controller only coordinates view-model validation and service calls.
/// </summary>
[Authorize]
public class WalletController : Controller
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    private long GetUserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// GET /Wallet — lists the current user's wallets.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var wallets = await _walletService.GetAllByUserIdAsync(GetUserId());
        return View(wallets);
    }

    /// <summary>
    /// GET /Wallet/Create — shows the empty create form.
    /// </summary>
    [HttpGet]
    public IActionResult Create() => View(new WalletCreateViewModel());

    /// <summary>
    /// POST /Wallet/Create — creates a wallet after validating the currency code.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WalletCreateViewModel model)
    {
        // Reject currencies not in CurrencyInfo.All (tampered requests / typos).
        var currency = CurrencyInfo.Find(model.Currency);
        if (currency == null)
            ModelState.AddModelError(nameof(model.Currency), "Unsupported currency.");

        if (!ModelState.IsValid) return View(model);

        // Always store the ISO code (e.g. "EUR"), not the symbol ("€").
        await _walletService.CreateWalletAsync(model.Name, model.Type, model.Balance, currency!.Code, GetUserId());

        TempData["SuccessMessage"] = "Wallet created.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// GET /Wallet/Edit/{id} — loads the wallet into the edit form.
    /// Same view-model as Create.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var wallet = await _walletService.GetByIdAsync(id, GetUserId());
        if (wallet == null) return NotFound();

        ViewData["WalletId"] = wallet.Id;
        return View(new WalletCreateViewModel
        {
            Name = wallet.Name,
            Type = wallet.Type,
            Balance = wallet.Balance,
            Currency = wallet.Currency
        });
    }

    /// <summary>
    /// POST /Wallet/Edit/{id} — updates name/type/balance/currency.
    ///
    /// Currency change is the tricky part:
    /// - If the currency changed, the service converts the balance and all transactions.
    /// - The user might ALSO have typed a new balance in the form. If they did, we keep
    ///   that value (balanceEdited = true) instead of overwriting it with the converted one.
    /// - If they did NOT edit the balance, we reload the wallet from the DB to read
    ///   the post-conversion balance (which may differ from the old balance in the form).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, WalletCreateViewModel model)
    {
        var userId = GetUserId();

        var newCurrency = CurrencyInfo.Find(model.Currency);
        if (newCurrency == null)
            ModelState.AddModelError(nameof(model.Currency), "Unsupported currency.");

        if (!ModelState.IsValid)
        {
            ViewData["WalletId"] = id;
            return View(model);
        }

        var existing = await _walletService.GetByIdAsync(id, userId);
        if (existing == null) return NotFound();

        string message = "Wallet updated.";

        bool currencyChanged = CurrencyInfo.Find(existing.Currency)?.Code != newCurrency!.Code;
        if (currencyChanged)
        {
            // Decide this BEFORE converting: did the user type a new balance himself?
            bool balanceEdited = model.Balance != existing.Balance;

            // Converts the balance and all transactions of this wallet (only if there is something to convert)
            var result = await _walletService.ChangeCurrencyAsync(userId, id, newCurrency.Code);
            if (!result.Success)
            {
                ModelState.AddModelError(nameof(model.Currency), result.Error!);
                ViewData["WalletId"] = id;
                return View(model);
            }

            if (!balanceEdited)
            {
                // Use the converted balance, not the old one from the form
                var converted = await _walletService.GetByIdAsync(id, userId);
                model.Balance = converted!.Balance;
            }

            if (result.Converted)
            {
                var rate = result.Rate.ToString("0.######", CultureInfo.InvariantCulture);
                message = $"Wallet updated and converted to {result.ToCode} at 1 {result.FromCode} = {rate} {result.ToCode}.";
            }
        }

        var updated = await _walletService.UpdateWalletAsync(
            id, model.Name, model.Type, model.Balance, newCurrency.Code, userId);
        if (updated == null) return NotFound();

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST /Wallet/Delete/{id} — deletes a wallet.
    ///
    /// IWalletService refuses to delete wallets that still have transactions:
    /// the database would silently cascade-delete those transactions, which is undesirable.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            if (!await _walletService.DeleteWalletAsync(id, GetUserId())) return NotFound();

            TempData["SuccessMessage"] = "Wallet deleted.";
        }
        catch (InvalidOperationException ex)
        {
            // The wallet still has transactions
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
