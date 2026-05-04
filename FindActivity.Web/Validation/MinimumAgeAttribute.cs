using System.ComponentModel.DataAnnotations;

namespace FindActivity.Web.Validation;

/// <summary>
/// Validates that a <see cref="DateTime"/> property is at least <see cref="MinimumAge"/> years
/// before <see cref="DateTime.UtcNow"/>. Treats null as invalid (pair with <see cref="RequiredAttribute"/>
/// if the field is optional).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class MinimumAgeAttribute : ValidationAttribute
{
    public int MinimumAge { get; }

    public MinimumAgeAttribute(int minimumAge)
    {
        MinimumAge = minimumAge;
        // ErrorMessage is consumed by ValidationAttribute via FormatErrorMessage.
        ErrorMessage = $"You must be at least {minimumAge} years old.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        if (value is not DateTime birthDate)
        {
            return new ValidationResult($"{validationContext.DisplayName} must be a date.");
        }

        // Reject obviously bogus dates (future / extreme past) as a side benefit.
        var today = DateTime.UtcNow.Date;
        if (birthDate.Date > today)
        {
            return new ValidationResult("Birth date cannot be in the future.");
        }
        if (birthDate.Year < 1900)
        {
            return new ValidationResult("Birth date is not valid.");
        }

        // Calculate age in completed years (handles leap-year edge case).
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
        {
            age--;
        }

        if (age < MinimumAge)
        {
            return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        return ValidationResult.Success;
    }
}
