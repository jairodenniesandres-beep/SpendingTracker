using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SpendingTracker.Data;
using SpendingTracker.Models;
using SpendingTracker.ViewModels;

namespace SpendingTracker.Controllers;

[Authorize]
public class ExpensesController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(AppDbContext context, ILogger<ExpensesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user has no NameIdentifier claim.");

    private IQueryable<Expense> OwnedExpenses() =>
        _context.Expenses.Where(e => e.OwnerUserId == CurrentUserId);

    private Task<Expense?> FindOwnedAsync(int id, bool tracking = true, bool includeCategory = false)
    {
        IQueryable<Expense> query = tracking ? OwnedExpenses() : OwnedExpenses().AsNoTracking();
        if (includeCategory) query = query.Include(e => e.Category);
        return query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IActionResult> Index([Bind(Prefix = "")] ExpenseFilterViewModel filter)
    {
        IQueryable<Expense> query = OwnedExpenses().AsNoTracking().Include(e => e.Category);

        if (ModelState.GetFieldValidationState(nameof(filter.PaymentMethod)) == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
        {
            ModelState.Remove(nameof(filter.PaymentMethod));
            ModelState.AddModelError(nameof(filter.PaymentMethod), "Unknown payment method - filter ignored.");
            filter.PaymentMethod = null;
        }
        var dateRangeValid = !(filter.From.HasValue && filter.To.HasValue && filter.From.Value.Date > filter.To.Value.Date);

        if (filter.CategoryId is > 0)
            query = query.Where(e => e.CategoryId == filter.CategoryId);
        else
            filter.CategoryId = null;

        if (filter.PaymentMethod.HasValue && Enum.IsDefined(filter.PaymentMethod.Value))
            query = query.Where(e => e.PaymentMethod == filter.PaymentMethod);

        if (dateRangeValid)
        {
            if (filter.From.HasValue) query = query.Where(e => e.Date >= filter.From.Value.Date);
            if (filter.To.HasValue) query = query.Where(e => e.Date <= filter.To.Value.Date);
        }

        var search = filter.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            if (search.Length > 100) search = search[..100];
            var lowered = search.ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(lowered) || (e.Notes != null && e.Notes.ToLower().Contains(lowered)));
            filter.Search = search;
        }

        var expenses = await query.ToListAsync();
        if (!ExpenseSort.All.Contains(filter.Sort)) filter.Sort = ExpenseSort.DateDesc;
        expenses = filter.Sort switch
        {
            ExpenseSort.DateAsc => expenses.OrderBy(e => e.Date).ThenBy(e => e.Id).ToList(),
            ExpenseSort.AmountDesc => expenses.OrderByDescending(e => e.Amount).ThenByDescending(e => e.Date).ToList(),
            ExpenseSort.AmountAsc => expenses.OrderBy(e => e.Amount).ThenByDescending(e => e.Date).ToList(),
            _ => expenses.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id).ToList(),
        };

        return View(new ExpenseIndexViewModel
        {
            Filter = filter,
            Expenses = expenses,
            Total = expenses.Sum(e => e.Amount),
            Categories = await CategorySelectListAsync(filter.CategoryId),
            SortOptions = new[]
            {
                new SelectListItem("Newest first", ExpenseSort.DateDesc, filter.Sort == ExpenseSort.DateDesc),
                new SelectListItem("Oldest first", ExpenseSort.DateAsc, filter.Sort == ExpenseSort.DateAsc),
                new SelectListItem("Amount: high to low", ExpenseSort.AmountDesc, filter.Sort == ExpenseSort.AmountDesc),
                new SelectListItem("Amount: low to high", ExpenseSort.AmountAsc, filter.Sort == ExpenseSort.AmountAsc),
            },
            IsFiltered = filter.CategoryId.HasValue || filter.PaymentMethod.HasValue || filter.From.HasValue
                         || filter.To.HasValue || !string.IsNullOrEmpty(filter.Search)
        });
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var expense = await FindOwnedAsync(id.Value, tracking: false, includeCategory: true);
        return expense == null ? NotFound() : View(expense);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new ExpenseFormViewModel { Date = DateTime.Today };
        vm.Categories = await CategorySelectListAsync(null);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(nameof(ExpenseFormViewModel.Title), nameof(ExpenseFormViewModel.Amount), nameof(ExpenseFormViewModel.Date),
              nameof(ExpenseFormViewModel.CategoryId), nameof(ExpenseFormViewModel.PaymentMethod), nameof(ExpenseFormViewModel.Notes))]
        ExpenseFormViewModel vm)
    {
        await ValidateCategoryAsync(vm.CategoryId);
        if (!ModelState.IsValid)
        {
            vm.Categories = await CategorySelectListAsync(vm.CategoryId);
            return View(vm);
        }

        var expense = new Expense { OwnerUserId = CurrentUserId, CreatedAt = DateTime.UtcNow };
        Map(vm, expense);

        try
        {
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ModelState.AddModelError(string.Empty, "The expense could not be saved. Please try again.");
            vm.Categories = await CategorySelectListAsync(vm.CategoryId);
            return View(vm);
        }

        TempData["Success"] = $"Expense \"{expense.Title}\" was added.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var expense = await FindOwnedAsync(id.Value, tracking: false);
        if (expense == null) return NotFound();
        var vm = ToViewModel(expense);
        vm.Categories = await CategorySelectListAsync(vm.CategoryId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id,
        [Bind(nameof(ExpenseFormViewModel.Id), nameof(ExpenseFormViewModel.Version), nameof(ExpenseFormViewModel.Title),
              nameof(ExpenseFormViewModel.Amount), nameof(ExpenseFormViewModel.Date), nameof(ExpenseFormViewModel.CategoryId),
              nameof(ExpenseFormViewModel.PaymentMethod), nameof(ExpenseFormViewModel.Notes))]
        ExpenseFormViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        var expense = await FindOwnedAsync(id);
        if (expense == null) return NotFound();

        await ValidateCategoryAsync(vm.CategoryId);
        if (!ModelState.IsValid)
        {
            vm.Categories = await CategorySelectListAsync(vm.CategoryId);
            return View(vm);
        }

        _context.Entry(expense).Property(e => e.Version).OriginalValue = vm.Version;
        Map(vm, expense);
        expense.Version = Guid.NewGuid();

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var current = await FindOwnedAsync(id, tracking: false);
            if (current == null) return NotFound();
            _logger.LogWarning(ex, "Concurrency conflict editing expense {Id}", id);
            vm.Version = current.Version;
            ModelState.Remove(nameof(vm.Version));
            ModelState.AddModelError(string.Empty,
                "This expense was changed by someone else after you opened it. " +
                "Check the values and click Save again to overwrite them.");
            vm.Categories = await CategorySelectListAsync(vm.CategoryId);
            return View(vm);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating expense {Id}", id);
            ModelState.AddModelError(string.Empty, "The changes could not be saved. Please try again.");
            vm.Categories = await CategorySelectListAsync(vm.CategoryId);
            return View(vm);
        }

        TempData["Success"] = $"Expense \"{expense.Title}\" was updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var expense = await FindOwnedAsync(id.Value, tracking: false, includeCategory: true);
        return expense == null ? NotFound() : View(expense);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed([FromRoute] int id)
    {
        var expense = await FindOwnedAsync(id);
        if (expense == null)
        {
            TempData["Success"] = "That expense was already deleted.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting expense {Id}", id);
            TempData["Error"] = "The expense could not be deleted. Please try again.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        TempData["Success"] = $"Expense \"{expense.Title}\" was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateCategoryAsync(int? categoryId)
    {
        if (categoryId.HasValue && !await _context.Categories.AnyAsync(c => c.Id == categoryId))
            ModelState.AddModelError(nameof(ExpenseFormViewModel.CategoryId), "Please select a valid category.");
    }

    private async Task<IEnumerable<SelectListItem>> CategorySelectListAsync(int? selectedId) =>
        (await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync())
            .Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == selectedId))
            .ToList();

    private static void Map(ExpenseFormViewModel vm, Expense expense)
    {
        expense.Title = vm.Title.Trim();
        expense.Amount = Math.Round(vm.Amount!.Value, 2, MidpointRounding.AwayFromZero);
        expense.Date = vm.Date!.Value.Date;
        expense.CategoryId = vm.CategoryId!.Value;
        expense.PaymentMethod = vm.PaymentMethod!.Value;
        expense.Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim();
    }

    private static ExpenseFormViewModel ToViewModel(Expense e) => new()
    {
        Id = e.Id,
        Version = e.Version,
        Title = e.Title,
        Amount = e.Amount,
        Date = e.Date,
        CategoryId = e.CategoryId,
        PaymentMethod = e.PaymentMethod,
        Notes = e.Notes
    };
}
