using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST2_2026_Team_6_FinanceWizard.Repositories;
using ST2_2026_Team_6_FinanceWizard.Repositories.Entities;

namespace ST2_2026_Team_6_FinanceWizard.Controllers;

public class CategoryController : Controller
{
    private readonly IEfRepository<Category> _categoryRepository;

    public CategoryController(IEfRepository<Category> categoryRepository)
    {
        _categoryRepository = categoryRepository;
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

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        if (!ModelState.IsValid) return View(category);

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}