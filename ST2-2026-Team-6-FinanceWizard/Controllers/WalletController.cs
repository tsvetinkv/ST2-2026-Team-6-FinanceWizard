using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;
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
        if (!ModelState.IsValid) return View(model);

        await _walletService.CreateWalletAsync(model.Name, model.Type, model.Balance, model.Currency, GetUserId());

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
        if (!ModelState.IsValid)
        {
            ViewData["WalletId"] = id;
            return View(model);
        }

        var updated = await _walletService.UpdateWalletAsync(id, model.Name, model.Type, model.Balance, model.Currency, GetUserId());
        if (updated == null) return NotFound();

        TempData["SuccessMessage"] = "Wallet updated.";
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
