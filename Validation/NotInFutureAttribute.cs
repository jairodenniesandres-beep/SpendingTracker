using System.ComponentModel.DataAnnotations;

namespace SpendingTracker.Validation;

/// <summary>Fails when a date is later than today (server time).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class NotInFutureAttribute : ValidationAttribute
{
    public NotInFutureAttribute() : base("{0} cannot be in the future.") { }

    public override bool IsValid(object? value) =>
        value is not DateTime date || date.Date <= DateTime.Today;
}
