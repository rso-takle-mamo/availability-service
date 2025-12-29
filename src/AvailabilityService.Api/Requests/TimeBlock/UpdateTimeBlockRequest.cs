using System.ComponentModel.DataAnnotations;
using AvailabilityService.Api.Attributes;

namespace AvailabilityService.Api.Requests.TimeBlock;

public class UpdateTimeBlockRequest
{
    [Required(ErrorMessage = "Start date and time is required")]
    public DateTime StartDateTime { get; set; }

    [Required(ErrorMessage = "End date and time is required")]
    public DateTime EndDateTime { get; set; }

    [Required(ErrorMessage = "Type is required")]
    [StringEnum("Vacation", "Break", "Custom", ErrorMessage = "Type must be one of: Vacation, Break, Custom")]
    public string Type { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public RecurrencePatternRequest? RecurrencePattern { get; set; }
}