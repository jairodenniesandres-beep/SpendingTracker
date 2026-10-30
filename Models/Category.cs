using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpendingTracker.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 1_000_000, ErrorMessage = "Monthly budget must be between 0 and 1,000,000.")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Monthly Budget")]
    public decimal? MonthlyBudget { get; set; }

    /// <summary>Optimistic concurrency token, regenerated on every update.</summary>
    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();

    public List<Expense> Expenses { get; set; } = new();
}
