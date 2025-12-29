using System.ComponentModel.DataAnnotations;
using AvailabilityService.Api.Attributes;

namespace AvailabilityService.Api.Requests.TimeBlock;

/// <summary>
/// Recurrence pattern for time blocks
/// </summary>
public class RecurrencePatternRequest
{
    /// <summary>
    /// Frequency of recurrence
    /// </summary>
    /// <example>Weekly</example>
    [Required(ErrorMessage = "Frequency is required")]
    [StringEnum("Daily", "Weekly", "Monthly", ErrorMessage = "Frequency must be one of: Daily, Weekly, Monthly")]
    public string Frequency { get; set; } = string.Empty;

    /// <summary>
    /// Interval between recurrences (e.g., 2 means every 2 occurrences)
    /// </summary>
    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "Interval must be a positive number")]
    public int? Interval { get; set; } = 1;

    /// <summary>
    /// Days of week for weekly pattern (0=Sunday, 1=Monday, ..., 6=Saturday)
    /// </summary>
    /// <example>[1, 3, 5]</example>
    // For weekly patterns - days of week (0=Sunday, 6=Saturday)
    public int[]? DaysOfWeek { get; set; }

    /// <summary>
    /// Days of month for monthly pattern (1-31 for specific days, -1 for last day, -2 for second to last, etc.)
    /// </summary>
    /// <example>[15, -1]</example>
    // For monthly patterns - days of month (1-31, or -1 for last day, -2 for second to last, etc.)
    public int[]? DaysOfMonth { get; set; }

    /// <summary>
    /// End date for recurrence (exclusive - recurrence stops before this date)
    /// </summary>
    /// <example>2025-12-31T23:59:59Z</example>
    // End condition - one of these is required but not both
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Maximum number of occurrences to create
    /// </summary>
    /// <example>10</example>
    public int? MaxOccurrences { get; set; }
}