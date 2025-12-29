using System.ComponentModel.DataAnnotations;

namespace AvailabilityService.Api.Requests.WorkingHours;

public class CreateWorkingHoursRequest
{
    /// <summary>
    /// The day of the week for working hours (0=Sunday, 1=Monday, ..., 6=Saturday)
    /// </summary>
    /// <example>1</example>
    [Required(ErrorMessage = "Day of week is required")]
    public DayOfWeek Day { get; set; }

    /// <summary>
    /// The start time for working hours
    /// </summary>
    /// <example>09:00:00</example>
    [Required(ErrorMessage = "Start time is required")]
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// The end time for working hours
    /// </summary>
    /// <example>17:00:00</example>
    [Required(ErrorMessage = "End time is required")]
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// Maximum number of concurrent bookings allowed
    /// </summary>
    /// <example>1</example>
    [Range(1, 100, ErrorMessage = "Max concurrent bookings must be between 1 and 100")]
    public int? MaxConcurrentBookings { get; set; } = 1;
}