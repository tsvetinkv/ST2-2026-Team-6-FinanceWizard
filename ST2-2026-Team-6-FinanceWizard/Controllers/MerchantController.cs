using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using ST2_2026_Team_6_FinanceWizard.Services.Contracts;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

/// <summary>
/// CRUD for merchants (the "where was it bought" dimension of a transaction).
///
/// Design patterns used:
/// - MVC Controller + Repository pattern.
/// - ILogoStorage abstracts file-system and HTTP concerns away from the controller
///   (single responsibility: the controller validates form data, the storage service
///   handles files, MIME sniffing and SSRF checks for image links).
/// </summary>
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

    /// <summary>
    /// GET /Merchant — lists all merchants, optionally filtered by name.
    /// </summary>
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

    /// <summary>
    /// GET /Merchant/Create — shows the empty create form.
    /// </summary>
    [HttpGet]
    public IActionResult Create() => View(new MerchantFormViewModel());

    /// <summary>
    /// POST /Merchant/Create — validates the form, saves the logo (if any), then saves the merchant.
    ///
    /// Order matters: the merchant entity stores the logo's relative path.
    /// If saving the merchant fails, the newly written logo file is deleted (rollback of the file system side).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MerchantFormViewModel model, CancellationToken cancellationToken)
    {
        model.Name = (model.Name ?? string.Empty).Trim();
        model.ImageLink = model.ImageLink?.Trim();

        await ValidateNameAsync(model.Name, excludeId: 0, cancellationToken);
        ValidateLogoChoice(model);

        if (!ModelState.IsValid) return View(model);

        // Either an uploaded photo, or a downloaded image, or nothing (logo stays null).
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

    /// <summary>
    /// GET /Merchant/Edit/{id} — loads the merchant into the edit form.
    /// </summary>
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

    /// <summary>
    /// POST /Merchant/Edit/{id} — updates the name and, optionally, the logo.
    ///
    /// Three possible logo changes:
    /// 1. New upload / link  → replace the logo.
    /// 2. "Remove logo" flag → set LogoUrl to null.
    /// 3. Neither            → keep the current logo.
    ///
    /// The old logo file is only deleted AFTER the DB change is saved — if the save fails,
    /// the old file is still there and the merchant still points to it.
    /// </summary>
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
            // The merchant update failed — do not leak the new file.
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

    /// <summary>
    /// POST /Merchant/Delete/{id} — removes a merchant.
    ///
    /// Like CategoryController.Delete, linked transactions are preserved:
    /// their MerchantId is set to null before the merchant is deleted.
    /// The logo file on disk is deleted last.
    /// </summary>
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

        // Only remove the file after the DB row is gone.
        _logoStorage.Delete(merchant.LogoUrl);

        TempData["SuccessMessage"] = $"Merchant \"{merchant.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Ensures the merchant name is non-empty and unique (case-sensitive per DB collation).
    /// </summary>
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

    /// <summary>
    /// Turns the posted logo choice (upload, link or none) into a LogoResult.
    /// Returns null when no new logo was given (the existing one is kept).
    /// </summary>
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

    /// <summary>
    /// Adds a logo error to the correct form field (upload vs link).
    /// </summary>
    private void AddLogoError(MerchantFormViewModel model, string message)
    {
        string key = model.Logo is { Length: > 0 }
            ? nameof(MerchantFormViewModel.Logo)
            : nameof(MerchantFormViewModel.ImageLink);
        ModelState.AddModelError(key, message);
    }
}
