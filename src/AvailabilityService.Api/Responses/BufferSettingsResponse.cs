namespace AvailabilityService.Api.Responses;

/// <summary>
/// Response model for buffer settings
/// </summary>
public class BufferSettingsResponse
{
    /// <summary>
    /// Buffer time in minutes before appointments
    /// </summary>
    /// <example>15</example>
    public int BufferBeforeMinutes { get; set; }

    /// <summary>
    /// Buffer time in minutes after appointments
    /// </summary>
    /// <example>15</example>
    public int BufferAfterMinutes { get; set; }
}