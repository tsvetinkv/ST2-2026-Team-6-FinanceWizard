using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

[Authorize]
public class MerchantController : Controller
{
    private readonly IEfRepository<Merchant> _merchantRepository;
    private readonly IEfRepository<Transaction> _transactionRepository;
    private readonly ILogoStorage _logoStorage;

    public MerchantController(
        IEfRepository<Merchant> merchantRepository,
        IEfRepository<Transaction> transactionRepository,
        ILogoStorage logoStorage)
    {
        _merchantRepository = merchantRepository;
        _transactionRepository = transactionRepository;
        _logoStorage = logoStorage;
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

    [HttpGet]
    public IActionResult Create() => View(new MerchantFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MerchantFormViewModel model, CancellationToken cancellationToken)
    {
        model.Name = (model.Name ?? string.Empty).Trim();
        model.ImageLink = model.ImageLink?.Trim();

        await ValidateNameAsync(model.Name, excludeId: 0, cancellationToken);
        ValidateLogoChoice(model);

        if (!ModelState.IsValid) return View(model);

        var logo = await ResolveLogoAsync(model, cancellationToken);
        if (logo is { Succeeded: false })
        {
            AddLogoError(model, logo.Error!);
            return View(model);
        }

        var merchant = new Merchant { Name = model.Name, LogoUrl = logo?.RelativePath };
        try
        {
            await _merchantRepository.AddAsync(merchant);
            await _merchantRepository.SaveChangesAsync();
        }
        catch
        {
            // Do not leave a logo file behind when the merchant could not be saved
            _logoStorage.Delete(logo?.RelativePath);
            throw;
        }

        TempData["SuccessMessage"] = $"Merchant \"{merchant.Name}\" added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var merchant = await _merchantRepository.All()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (merchant == null) return NotFound();

        ViewData["MerchantId"] = merchant.Id;
        return View(new MerchantFormViewModel
        {
            Name = merchant.Name,
            CurrentLogoUrl = merchant.LogoUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, MerchantFormViewModel model, CancellationToken cancellationToken)
    {
        var merchant = await _merchantRepository.GetByIdAsync(id);
        if (merchant == null) return NotFound();

        model.Name = (model.Name ?? string.Empty).Trim();
        model.ImageLink = model.ImageLink?.Trim();
        model.CurrentLogoUrl = merchant.LogoUrl;

        await ValidateNameAsync(model.Name, excludeId: id, cancellationToken);
        ValidateLogoChoice(model);

        if (!ModelState.IsValid)
        {
            ViewData["MerchantId"] = id;
            return View(model);
        }

        var logo = await ResolveLogoAsync(model, cancellationToken);
        if (logo is { Succeeded: false })
        {
            ViewData["MerchantId"] = id;
            AddLogoError(model, logo.Error!);
            return View(model);
        }

        string? oldLogo = merchant.LogoUrl;
        merchant.Name = model.Name;
        if (logo is { Succeeded: true })
        {
            merchant.LogoUrl = logo.RelativePath;
        }
        else if (model.RemoveLogo)
        {
            merchant.LogoUrl = null;
        }

        try
        {
            await _merchantRepository.SaveChangesAsync();
        }
        catch
        {
            _logoStorage.Delete(logo?.RelativePath);
            throw;
        }

        // The old logo file is only removed after the change has been saved
        if (!string.Equals(oldLogo, merchant.LogoUrl, StringComparison.Ordinal))
        {
            _logoStorage.Delete(oldLogo);
        }

        TempData["SuccessMessage"] = $"Merchant \"{merchant.Name}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var merchant = await _merchantRepository.GetByIdAsync(id);
        if (merchant == null) return NotFound();

        // Transactions keep their amount and date; only their merchant link is removed
        var linked = await _transactionRepository.All()
            .Where(t => t.MerchantId == id)
            .ToListAsync(cancellationToken);
        foreach (var transaction in linked)
        {
            transaction.MerchantId = null;
        }

        _merchantRepository.Delete(merchant);
        await _merchantRepository.SaveChangesAsync();

        _logoStorage.Delete(merchant.LogoUrl);

        TempData["SuccessMessage"] = $"Merchant \"{merchant.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateNameAsync(string name, long excludeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(name))
        {
            ModelState.AddModelError(nameof(MerchantFormViewModel.Name), "Please enter a merchant name.");
        }
        else if (await _merchantRepository.All()
                     .AnyAsync(m => m.Name == name && m.Id != excludeId, cancellationToken))
        {
            ModelState.AddModelError(nameof(MerchantFormViewModel.Name), "A merchant with this name already exists.");
        }
    }

    // A photo and a link together are ambiguous, so only one of them may be given
    private void ValidateLogoChoice(MerchantFormViewModel model)
    {
        bool hasPhoto = model.Logo is { Length: > 0 };
        bool hasLink = !string.IsNullOrEmpty(model.ImageLink);

        if (hasPhoto && hasLink)
        {
            ModelState.AddModelError(nameof(MerchantFormViewModel.ImageLink),
                "Choose either a photo or a link, not both.");
        }
    }

    // Returns null when no new logo was given (the existing one is kept)
    private async Task<LogoResult?> ResolveLogoAsync(MerchantFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Logo is { Length: > 0 })
        {
            return await _logoStorage.SaveUploadAsync(model.Logo, cancellationToken);
        }

        if (!string.IsNullOrEmpty(model.ImageLink))
        {
            return await _logoStorage.DownloadFromLinkAsync(model.ImageLink, cancellationToken);
        }

        return null;
    }

    private void AddLogoError(MerchantFormViewModel model, string message)
    {
        string key = model.Logo is { Length: > 0 }
            ? nameof(MerchantFormViewModel.Logo)
            : nameof(MerchantFormViewModel.ImageLink);
        ModelState.AddModelError(key, message);
    }
}
