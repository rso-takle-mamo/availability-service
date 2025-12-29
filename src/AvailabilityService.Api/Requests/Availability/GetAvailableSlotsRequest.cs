using System.ComponentModel.DataAnnotations;

namespace AvailabilityService.Api.Requests.Availability;

public class GetAvailableSlotsRequest
{
    public Guid? TenantId { get; set; }
    
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }
}