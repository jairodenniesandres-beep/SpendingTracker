using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpendingTracker.Data;
using SpendingTracker.ViewModels;

namespace SpendingTracker.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context) => _context = context;

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user has no NameIdentifier claim.");

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var sixMonthsStart = thisMonthStart.AddMonths(-5);
        var yearStart = new DateTime(today.Year, 1, 1);
        var rangeStart = sixMonthsStart < yearStart ? sixMonthsStart : yearStart;
        var ownerId = CurrentUserId;

        var window = await _context.Expenses.AsNoTracking()
            .Where(e => e.OwnerUserId == ownerId && e.Date >= rangeStart)
            .Select(e => new { e.Date, e.Amount, e.CategoryId })
            .ToListAsync();

        var thisMonth = window.Where(e => e.Date >= thisMonthStart && e.Date < thisMonthStart.AddMonths(1)).ToList();
        var lastMonthTotal = window.Where(e => e.Date >= lastMonthStart && e.Date < thisMonthStart).Sum(e => e.Amount);
        var thisMonthTotal = thisMonth.Sum(e => e.Amount);

        var categories = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var spentByCategory = thisMonth.GroupBy(e => e.CategoryId).ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        return View(new DashboardViewModel
        {
            Today = today,
            ThisMonthTotal = thisMonthTotal,
            LastMonthTotal = lastMonthTotal,
            ChangePercent = lastMonthTotal == 0 ? null : Math.Round((thisMonthTotal - lastMonthTotal) / lastMonthTotal * 100, 1),
            ThisYearTotal = window.Where(e => e.Date >= yearStart).Sum(e => e.Amount),
            ExpenseCount = await _context.Expenses.CountAsync(e => e.OwnerUserId == ownerId),
            ThisMonthCount = thisMonth.Count,
            AveragePerDayThisMonth = Math.Round(thisMonthTotal / today.Day, 2),
            TotalBudget = categories.Any(c => c.MonthlyBudget.HasValue) ? categories.Sum(c => c.MonthlyBudget ?? 0) : null,
            CategorySpending = categories
                .Select(c => new CategorySpending
                {
                    CategoryId = c.Id,
                    Name = c.Name,
                    Budget = c.MonthlyBudget,
                    Spent = spentByCategory.GetValueOrDefault(c.Id)
                })
                .Where(c => c.Spent > 0 || c.Budget.HasValue)
                .OrderByDescending(c => c.Spent).ThenBy(c => c.Name)
                .ToList(),
            LastSixMonths = Enumerable.Range(0, 6)
                .Select(i => thisMonthStart.AddMonths(-i))
                .Select(m => new MonthTotal
                {
                    Month = m,
                    Total = window.Where(e => e.Date >= m && e.Date < m.AddMonths(1)).Sum(e => e.Amount),
                    Count = window.Count(e => e.Date >= m && e.Date < m.AddMonths(1))
                })
                .ToList(),
            RecentExpenses = await _context.Expenses.AsNoTracking().Include(e => e.Category)
                .Where(e => e.OwnerUserId == ownerId)
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
                .Take(5).ToListAsync()
        });
    }
}
