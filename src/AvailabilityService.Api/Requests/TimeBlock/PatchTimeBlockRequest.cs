using System.ComponentModel.DataAnnotations;
using AvailabilityService.Api.Attributes;

namespace AvailabilityService.Api.Requests.TimeBlock;

public class PatchTimeBlockRequest
{
    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    [StringEnum("Vacation", "Break", "Custom", ErrorMessage = "Type must be one of: Vacation, Break, Custom")]
    public string? Type { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}