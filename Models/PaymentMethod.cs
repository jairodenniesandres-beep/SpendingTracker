using System.ComponentModel.DataAnnotations;

namespace SpendingTracker.Models;

public enum PaymentMethod
{
    Cash,
    [Display(Name = "Debit Card")] DebitCard,
    [Display(Name = "Credit Card")] CreditCard,
    [Display(Name = "Bank Transfer")] BankTransfer,
    Other
}
