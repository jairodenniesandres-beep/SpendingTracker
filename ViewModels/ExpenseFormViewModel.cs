using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SpendingTracker.Models;
using SpendingTracker.Validation;

namespace SpendingTracker.ViewModels;

/// <summary>Used by Create and Edit. Only the fields a user may change are bindable.</summary>
public class ExpenseFormViewModel : IValidatableObject
{
    public static readonly DateTime MinDate = new(2000, 1, 1);

    public int Id { get; set; }

    public Guid Version { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 100 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required.")]
    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Amount must be between 0.01 and 1,000,000.",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [DataType(DataType.Currency)]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Date is required.")]
    [DataType(DataType.Date)]
    [NotInFuture(ErrorMessage = "Date cannot be in the future.")]
    public DateTime? Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Please select a payment method.")]
    [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Please select a valid payment method.")]
    [Display(Name = "Payment Method")]
    public PaymentMethod? PaymentMethod { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> Categories { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Date.HasValue && Date.Value.Date < MinDate)
        {
            yield return new ValidationResult("Date must be on or after 1 January 2000.", new[] { nameof(Date) });
        }
    }
}
