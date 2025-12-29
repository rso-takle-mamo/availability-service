using System.ComponentModel.DataAnnotations;
using AvailabilityService.Api.Attributes;

namespace AvailabilityService.Api.Requests.TimeBlock;

/// <summary>
    /// Request to create a time block (unavailable period)
    /// </summary>
public class CreateTimeBlockRequest
{
    /// <summary>
    /// Start date and time of the time block
    /// </summary>
    /// <example>2025-12-25T09:00:00Z</example>
    [Required(ErrorMessage = "Start date and time is required")]
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// End date and time of the time block
    /// </summary>
    /// <example>2025-12-25T17:00:00Z</example>
    [Required(ErrorMessage = "End date and time is required")]
    public DateTime EndDateTime { get; set; }

    /// <summary>
    /// Type of time block
    /// </summary>
    /// <example>Vacation</example>
    [Required(ErrorMessage = "Type is required")]
    [StringEnum("Vacation", "Break", "Custom", ErrorMessage = "Type must be one of: Vacation, Break, Custom")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Optional reason or description for the time block
    /// </summary>
    /// <example>Christmas holidays</example>
    public string? Reason { get; set; }

    /// <summary>
    /// Optional recurrence pattern for recurring time blocks
    /// </summary>
    public RecurrencePatternRequest? RecurrencePattern { get; set; }
}