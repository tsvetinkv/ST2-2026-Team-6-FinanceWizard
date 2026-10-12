using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Models;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;
using System.Text.RegularExpressions;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

[Authorize]
public class TagController : Controller
{
    // Icon names come from the Bootstrap Icons library (letters, digits and dashes only)
    private static readonly Regex IconNamePattern = new("^[a-z0-9-]{1,60}$", RegexOptions.Compiled);

    private const string DefaultColor = "#1d4ed8";

    private readonly IEfRepository<Tag> _tagRepository;

    public TagController(IEfRepository<Tag> tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<IActionResult> Index(string? searchName, CancellationToken cancellationToken)
    {
        var query = _tagRepository.All().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            query = query.Where(t => t.Name.Contains(searchName));
            ViewData["SearchName"] = searchName;
        }

        var tags = await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
        return View(tags);
    }

    [HttpGet]
    public IActionResult Create() => View(new Tag { Color = DefaultColor });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Color,Icon")] Tag tag, CancellationToken cancellationToken)
    {
        if (!await ApplyFormAsync(tag, excludeId: 0, cancellationToken))
        {
            return View(tag);
        }

        await _tagRepository.AddAsync(tag);
        await _tagRepository.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Tag \"#{tag.Name}\" added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.All()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tag == null) return NotFound();

        ViewData["TagId"] = tag.Id;
        return View(tag);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, [Bind("Name,Color,Icon")] Tag input, CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null) return NotFound();

        if (!await ApplyFormAsync(input, excludeId: id, cancellationToken))
        {
            ViewData["TagId"] = id;
            return View(input);
        }

        tag.Name = input.Name;
        tag.Color = input.Color;
        tag.Icon = input.Icon;
        await _tagRepository.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Tag \"#{tag.Name}\" updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag == null) return NotFound();

        // Links from transactions to this tag are removed by the database (cascade delete)
        _tagRepository.Delete(tag);
        await _tagRepository.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Tag \"#{tag.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }

    // Cleans and checks the posted values. Returns false (with errors in ModelState) when invalid.
    private async Task<bool> ApplyFormAsync(Tag tag, long excludeId, CancellationToken cancellationToken)
    {
        ModelState.Remove(nameof(Tag.Name));  // re-validated below after cleaning
        ModelState.Remove(nameof(Tag.Color)); // validated below
        ModelState.Remove(nameof(Tag.Icon));  // validated below

        // Tags are shown with a leading '#', so strip it if the user typed one
        tag.Name = (tag.Name ?? string.Empty).Trim().TrimStart('#').Trim();
        if (tag.Name.Length == 0)
        {
            ModelState.AddModelError(nameof(Tag.Name), "Please enter a tag name.");
        }
        else if (tag.Name.Length > 50)
        {
            ModelState.AddModelError(nameof(Tag.Name), "The name cannot exceed 50 characters.");
        }
        else if (await _tagRepository.All()
                     .AnyAsync(t => t.Name == tag.Name && t.Id != excludeId, cancellationToken))
        {
            ModelState.AddModelError(nameof(Tag.Name), "A tag with this name already exists.");
        }

        // Colour is required and must be a real "#rrggbb" value
        var color = Colors.Normalize(tag.Color);
        if (color == null)
        {
            ModelState.AddModelError(nameof(Tag.Color), "Please choose a color for the tag.");
        }
        else
        {
            tag.Color = color;
        }

        // Icon is optional: an empty value means no icon
        var icon = (tag.Icon ?? string.Empty).Trim().ToLowerInvariant();
        if (icon.Length == 0)
        {
            tag.Icon = null;
        }
        else if (!IconNamePattern.IsMatch(icon))
        {
            ModelState.AddModelError(nameof(Tag.Icon), "Please choose a valid icon.");
        }
        else
        {
            tag.Icon = icon;
        }

        return ModelState.IsValid;
    }
}
