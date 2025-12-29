using System.ComponentModel.DataAnnotations;

namespace AvailabilityService.Api.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class StringEnumAttribute : ValidationAttribute
{
    private readonly string[] _allowedValues;

    public StringEnumAttribute(params string[] allowedValues)
    {
        _allowedValues = allowedValues ?? throw new ArgumentNullException(nameof(allowedValues));
        ErrorMessage = "Invalid value. Allowed values are: " + string.Join(", ", allowedValues);
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success; // Use Required attribute for null checks
        }

        if (value is not string stringValue)
        {
            return new ValidationResult("Value must be a string.");
        }

        // Case-insensitive comparison
        if (_allowedValues.Contains(stringValue, StringComparer.OrdinalIgnoreCase))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage);
    }
}