using SpendingTracker.Models;

namespace SpendingTracker.ViewModels;

public class DashboardViewModel
{
    public DateTime Today { get; set; }
    public decimal ThisMonthTotal { get; set; }
    public decimal LastMonthTotal { get; set; }
    /// <summary>Null when last month had no spending (change is undefined).</summary>
    public decimal? ChangePercent { get; set; }
    public decimal ThisYearTotal { get; set; }
    public int ExpenseCount { get; set; }
    public int ThisMonthCount { get; set; }
    public decimal AveragePerDayThisMonth { get; set; }
    public decimal? TotalBudget { get; set; }

    public IList<CategorySpending> CategorySpending { get; set; } = new List<CategorySpending>();
    public IList<MonthTotal> LastSixMonths { get; set; } = new List<MonthTotal>();
    public IList<Expense> RecentExpenses { get; set; } = new List<Expense>();
}

public class CategorySpending
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Spent { get; set; }
    public decimal? Budget { get; set; }
    public decimal? Remaining => Budget.HasValue ? Budget.Value - Spent : null;
    public decimal? PercentUsed => Budget is > 0 ? Math.Round(Spent / Budget.Value * 100, 1) : null;
}

public class MonthTotal
{
    public DateTime Month { get; set; }
    public decimal Total { get; set; }
    public int Count { get; set; }
}
