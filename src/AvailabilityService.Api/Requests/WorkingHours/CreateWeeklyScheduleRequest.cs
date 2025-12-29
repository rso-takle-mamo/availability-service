using System.ComponentModel.DataAnnotations;

namespace AvailabilityService.Api.Requests.WorkingHours;

/// <summary>
    /// Request to create a weekly schedule in bulk
    /// </summary>
public class CreateWeeklyScheduleRequest
{
    /// <summary>
    /// List of schedule entries for different days
    /// </summary>
    [Required(ErrorMessage = "Schedule is required")]
    [MinLength(1, ErrorMessage = "At least one schedule entry is required")]
    public List<WeeklyScheduleEntry> Schedule { get; set; } = new();
}

/// <summary>
    /// Entry representing working hours for specific days
    /// </summary>
public class WeeklyScheduleEntry
{
    /// <summary>
    /// Days of the week this entry applies to (0=Sunday, 1=Monday, ..., 6=Saturday)
    /// </summary>
    /// <example>[1, 2, 3]</example>
    [Required(ErrorMessage = "Days are required")]
    [MinLength(1, ErrorMessage = "At least one day must be specified")]
    public List<DayOfWeek> Days { get; set; } = new();

    /// <summary>
    /// Start time for work days (required when IsWorkFree is false)
    /// </summary>
    /// <example>09:00:00</example>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// End time for work days (required when IsWorkFree is false)
    /// </summary>
    /// <example>17:00:00</example>
    public TimeOnly? EndTime { get; set; }

    /// <summary>
    /// If true, these days are marked as free days and no working hours will be created.
    /// If false or not provided, StartTime and EndTime must be specified.
    /// </summary>
    /// <example>false</example>
    public bool IsWorkFree { get; set; } = false;

    /// <summary>
    /// Maximum number of concurrent bookings allowed
    /// </summary>
    /// <example>1</example>
    [Range(1, 100, ErrorMessage = "Max concurrent bookings must be between 1 and 100")]
    public int? MaxConcurrentBookings { get; set; } = 1;
}