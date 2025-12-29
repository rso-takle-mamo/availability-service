using AvailabilityService.Api.Models;

namespace AvailabilityService.Api.Exceptions;

public class ValidationException(string message, List<ValidationError> validationErrors) : BaseDomainException(message)
{
    public override string ErrorCode => "VALIDATION_ERROR";
    
    public List<ValidationError> ValidationErrors { get; } = validationErrors;

    public ValidationException(List<ValidationError> validationErrors)
        : this($"Validation failed with {validationErrors.Count} error(s).", validationErrors)
    {
    }
}