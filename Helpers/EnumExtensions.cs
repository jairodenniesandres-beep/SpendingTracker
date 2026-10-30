using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SpendingTracker.Helpers;

public static class EnumExtensions
{
    /// <summary>Returns the [Display(Name)] of an enum value, or its name.</summary>
    public static string GetDisplayName(this Enum value) =>
        value.GetType().GetMember(value.ToString()).FirstOrDefault()
            ?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
}
