using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

public class WalletController : Controller
{
    private readonly IEfRepository<Wallet> _walletRepository;

    public WalletController(IEfRepository<Wallet> walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var wallets = await _walletRepository.All().AsNoTracking()
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

        ViewBag.TotalBalance = wallets.Sum(w => w.Balance);
        return View(wallets);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Wallet wallet)
    {
        if (!ModelState.IsValid) return View(wallet);

        await _walletRepository.AddAsync(wallet);
        await _walletRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}