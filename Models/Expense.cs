using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SpendingTracker.Models;

public class Expense
{
    public int Id { get; set; }

    /// <summary>Owning Identity user. Expenses are private — never shared with other users.</summary>
    [Required]
    public string OwnerUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(OwnerUserId))]
    public IdentityUser? Owner { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Display(Name = "Category")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Created")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();
}
