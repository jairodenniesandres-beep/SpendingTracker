using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using SpendingTracker.Models;

namespace SpendingTracker.ViewModels;

public static class ExpenseSort
{
    public const string DateDesc = "date_desc";
    public const string DateAsc = "date_asc";
    public const string AmountDesc = "amount_desc";
    public const string AmountAsc = "amount_asc";

    public static readonly string[] All = { DateDesc, DateAsc, AmountDesc, AmountAsc };
}

public class ExpenseFilterViewModel : IValidatableObject
{
    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Display(Name = "Payment Method")]
    public PaymentMethod? PaymentMethod { get; set; }

    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    [StringLength(100, ErrorMessage = "Search text cannot exceed 100 characters.")]
    public string? Search { get; set; }

    public string? Sort { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From.Value.Date > To.Value.Date)
        {
            yield return new ValidationResult("The 'From' date must be on or before the 'To' date.", new[] { nameof(From) });
        }
    }
}

public class ExpenseIndexViewModel
{
    public ExpenseFilterViewModel Filter { get; set; } = new();

    [ValidateNever] public IList<Expense> Expenses { get; set; } = new List<Expense>();
    [ValidateNever] public decimal Total { get; set; }
    [ValidateNever] public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();
    [ValidateNever] public IEnumerable<SelectListItem> SortOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    [ValidateNever] public bool IsFiltered { get; set; }
}
