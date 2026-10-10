using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

public class MerchantController : Controller
{
    private readonly IEfRepository<Merchant> _merchantRepository;

    public MerchantController(IEfRepository<Merchant> merchantRepository)
    {
        _merchantRepository = merchantRepository;
    }

    public async Task<IActionResult> Index(string? searchName, CancellationToken cancellationToken)
    {
        var query = _merchantRepository.All().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            query = query.Where(m => m.Name.Contains(searchName));
            ViewData["SearchName"] = searchName;
        }

        var merchants = await query.OrderBy(m => m.Name).ToListAsync(cancellationToken);
        return View(merchants);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Merchant merchant)
    {
        if (!ModelState.IsValid) return View(merchant);

        await _merchantRepository.AddAsync(merchant);
        await _merchantRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}