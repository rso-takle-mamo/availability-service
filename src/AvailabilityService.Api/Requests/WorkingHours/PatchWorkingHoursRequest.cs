using System.ComponentModel.DataAnnotations;

namespace AvailabilityService.Api.Requests.WorkingHours;

public class PatchWorkingHoursRequest
{
    /// <summary>
    /// Start time (HH:mm format)
    /// </summary>
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Start time must be in HH:mm format")]
    public string? StartTime { get; set; }

    /// <summary>
    /// End time (HH:mm format)
    /// </summary>
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "End time must be in HH:mm format")]
    public string? EndTime { get; set; }

    /// <summary>
    /// Maximum number of concurrent bookings allowed
    /// </summary>
    [Range(1, 100, ErrorMessage = "Max concurrent bookings must be between 1 and 100")]
    public int? MaxConcurrentBookings { get; set; }
}