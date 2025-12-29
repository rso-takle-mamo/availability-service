using System.ComponentModel.DataAnnotations;

namespace AvailabilityService.Api.Requests.WorkingHours;

public class UpdateWorkingHoursRequest
{
    [Required(ErrorMessage = "Start time is required")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    public TimeOnly EndTime { get; set; }

    [Range(1, 100, ErrorMessage = "Max concurrent bookings must be between 1 and 100")]
    public int? MaxConcurrentBookings { get; set; }
}