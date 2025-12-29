namespace AvailabilityService.Api.Exceptions;

public class NotFoundException(string resourceType, object resourceId)
    : BaseDomainException($"{resourceType} with ID '{resourceId}' was not found.")
{
    public override string ErrorCode => "NOT_FOUND";
    
    public string ResourceType { get; } = resourceType;

    public object ResourceId { get; } = resourceId;
}