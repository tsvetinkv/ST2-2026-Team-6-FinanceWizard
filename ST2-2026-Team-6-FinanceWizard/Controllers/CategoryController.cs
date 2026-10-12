using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using System.Text.RegularExpressions;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

[Authorize]
public class CategoryController : Controller
{
    // Icon names come from the Bootstrap Icons library (letters, digits and dashes only)
    private static readonly Regex IconNamePattern = new("^[a-z0-9-]{1,60}$", RegexOptions.Compiled);

    private readonly IEfRepository<Category> _categoryRepository;
    private readonly IEfRepository<Transaction> _transactionRepository;

    public CategoryController(
        IEfRepository<Category> categoryRepository,
        IEfRepository<Transaction> transactionRepository)
    {
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<IActionResult> Index(string? searchName, CancellationToken cancellationToken)
    {
        var query = _categoryRepository.All().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            query = query.Where(c => c.Name.Contains(searchName));
            ViewData["SearchName"] = searchName;
        }

        var categories = await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create() => View(new Category { Icon = "tag" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Icon,Color")] Category category, CancellationToken cancellationToken)
    {
        if (!await ApplyFormAsync(category, excludeId: 0, cancellationToken))
        {
            return View(category);
        }

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Category \"{category.Name}\" added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.All()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category == null) return NotFound();

        ViewData["CategoryId"] = category.Id;
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, [Bind("Name,Icon,Color")] Category input, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();

        if (!await ApplyFormAsync(input, excludeId: id, cancellationToken))
        {
            ViewData["CategoryId"] = id;
            return View(input);
        }

        category.Name = input.Name;
        category.Icon = input.Icon;
        category.Color = input.Color;
        await _categoryRepository.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Category \"{category.Name}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();

        // Transactions keep their amount and date; only their category link is removed
        var linked = await _transactionRepository.All()
            .Where(t => t.CategoryId == id)
            .ToListAsync(cancellationToken);
        foreach (var transaction in linked)
        {
            transaction.CategoryId = null;
        }

        _categoryRepository.Delete(category);
        await _categoryRepository.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Category \"{category.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }

    // Cleans and checks the posted values. Returns false (with errors in ModelState) when invalid.
    private async Task<bool> ApplyFormAsync(Category category, long excludeId, CancellationToken cancellationToken)
    {
        ModelState.Remove(nameof(Category.Name)); // re-validated below after trimming
        ModelState.Remove(nameof(Category.Icon)); // validated below
        ModelState.Remove(nameof(Category.Color)); // validated below

        category.Name = (category.Name ?? string.Empty).Trim();

        var icon = (category.Icon ?? string.Empty).Trim().ToLowerInvariant();
        if (icon.Length == 0)
        {
            icon = "tag";
        }
        else if (!IconNamePattern.IsMatch(icon))
        {
            ModelState.AddModelError(nameof(Category.Icon), "Please choose a valid icon.");
        }
        category.Icon = icon;

        // Colour is optional (empty = automatic colour), but must be a real "#rrggbb" value when given
        if (string.IsNullOrWhiteSpace(category.Color))
        {
            category.Color = null;
        }
        else
        {
            category.Color = Colors.Normalize(category.Color);
            if (category.Color == null)
            {
                ModelState.AddModelError(nameof(Category.Color), "Please choose a valid color.");
            }
        }

        if (string.IsNullOrEmpty(category.Name))
        {
            ModelState.AddModelError(nameof(Category.Name), "Please enter a category name.");
        }
        else if (await _categoryRepository.All()
                     .AnyAsync(c => c.Name == category.Name && c.Id != excludeId, cancellationToken))
        {
            ModelState.AddModelError(nameof(Category.Name), "A category with this name already exists.");
        }

        return ModelState.IsValid;
    }
}
