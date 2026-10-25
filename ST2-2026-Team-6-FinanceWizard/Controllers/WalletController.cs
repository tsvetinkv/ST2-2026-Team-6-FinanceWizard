using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
using System.Globalization;
using System.Security.Claims;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

[Authorize]
public class WalletController : Controller
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    private long GetUserId() => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var wallets = await _walletService.GetAllByUserIdAsync(GetUserId());
        return View(wallets);
    }

    [HttpGet]
    public IActionResult Create() => View(new WalletCreateViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WalletCreateViewModel model)
    {
        var currency = CurrencyInfo.Find(model.Currency);
        if (currency == null)
            ModelState.AddModelError(nameof(model.Currency), "Unsupported currency.");

        if (!ModelState.IsValid) return View(model);

        // Always store the ISO code
        await _walletService.CreateWalletAsync(model.Name, model.Type, model.Balance, currency!.Code, GetUserId());

        TempData["SuccessMessage"] = "Wallet created.";
        return RedirectToAction(nameof(Index));
    }
    // Same form as "New wallet", filled with the current values
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
