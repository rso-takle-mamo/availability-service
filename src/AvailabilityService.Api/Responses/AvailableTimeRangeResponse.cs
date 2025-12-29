namespace AvailabilityService.Api.Responses;

/// <summary>
/// Response model containing available time ranges for booking
/// </summary>
public class AvailableTimeRangeResponse
{
    /// <summary>
    /// List of available time ranges
    /// </summary>
    public List<AvailableTimeRange> AvailableRanges { get; set; } = new();
}

/// <summary>
/// Represents an available time range for booking
/// </summary>
public class AvailableTimeRange
{
    /// <example>2025-12-10T09:00:00Z</example>
    public DateTime Start { get; set; }

    /// <example>2025-12-10T10:00:00Z</example>
    public DateTime End { get; set; }
}