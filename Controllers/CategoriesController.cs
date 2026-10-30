using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendingTracker.Data;
using SpendingTracker.Models;

namespace SpendingTracker.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(AppDbContext context, ILogger<CategoriesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user has no NameIdentifier claim.");

    public async Task<IActionResult> Index()
    {
        var ownerId = CurrentUserId;
        var categories = await _context.Categories.AsNoTracking()
            .Select(c => new { Category = c, Count = c.Expenses.Count(e => e.OwnerUserId == ownerId) })
            .ToListAsync();
        ViewBag.ExpenseCounts = categories.ToDictionary(x => x.Category.Id, x => x.Count);
        return View(categories.Select(x => x.Category).OrderBy(c => c.Name).ToList());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();
        ViewBag.ExpenseCount = await _context.Expenses.CountAsync(e => e.CategoryId == id && e.OwnerUserId == CurrentUserId);
        return View(category);
    }

    public IActionResult Create() => View(new Category());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(nameof(Category.Name), nameof(Category.MonthlyBudget))] Category category)
    {
        category.Name = category.Name?.Trim() ?? string.Empty;
        await ValidateUniqueNameAsync(category.Name, null);
        if (!ModelState.IsValid) return View(category);

        try
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error creating category");
            ModelState.AddModelError(string.Empty, "The category could not be saved. Please try again.");
            return View(category);
        }

        TempData["Success"] = $"Category \"{category.Name}\" was created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return category == null ? NotFound() : View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id,
        [Bind(nameof(Category.Id), nameof(Category.Version), nameof(Category.Name), nameof(Category.MonthlyBudget))] Category input)
    {
        if (id != input.Id) return NotFound();

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();

        input.Name = input.Name?.Trim() ?? string.Empty;
        await ValidateUniqueNameAsync(input.Name, id);
        if (!ModelState.IsValid) return View(input);

        _context.Entry(category).Property(c => c.Version).OriginalValue = input.Version;
        category.Name = input.Name;
        category.MonthlyBudget = input.MonthlyBudget;
        category.Version = Guid.NewGuid();

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var current = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (current == null) return NotFound();
            _logger.LogWarning(ex, "Concurrency conflict editing category {Id}", id);
            input.Version = current.Version;
            ModelState.Remove(nameof(input.Version));
            ModelState.AddModelError(string.Empty,
                "This category was changed by someone else after you opened it. " +
                "Check the values and click Save again to overwrite them.");
            return View(input);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating category {Id}", id);
            ModelState.AddModelError(string.Empty, "The changes could not be saved. Please try again.");
            return View(input);
        }

        TempData["Success"] = $"Category \"{category.Name}\" was updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();
        // Show caller's own count; delete still checks all users (shared category).
        ViewBag.ExpenseCount = await _context.Expenses.CountAsync(e => e.CategoryId == id && e.OwnerUserId == CurrentUserId);
        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            TempData["Success"] = "That category was already deleted.";
            return RedirectToAction(nameof(Index));
        }

        var expenseCount = await _context.Expenses.CountAsync(e => e.CategoryId == id);
        if (expenseCount > 0)
        {
            TempData["Error"] = $"\"{category.Name}\" still has {expenseCount} expense(s). Move or delete them before deleting the category.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        try
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting category {Id}", id);
            TempData["Error"] = "The category could not be deleted. Please try again.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Success"] = $"Category \"{category.Name}\" was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateUniqueNameAsync(string name, int? excludeId)
    {
        if (string.IsNullOrEmpty(name)) return;
        var lowered = name.ToLower();
        if (await _context.Categories.AnyAsync(c => c.Name.ToLower() == lowered && c.Id != excludeId))
            ModelState.AddModelError(nameof(Category.Name), "A category with this name already exists.");
    }
}
