using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

public class TagController : Controller
{
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

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Tag tag)
    {
        if (!ModelState.IsValid) return View(tag);

        await _tagRepository.AddAsync(tag);
        await _tagRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}