namespace AvailabilityService.Api.Models.Internal;

/// <summary>
/// Result of an availability check including conflicts
/// </summary>
public class AvailabilityCheckResult
{
    public bool IsAvailable { get; set; }
    public List<ConflictInfo> Conflicts { get; set; } = new();
}