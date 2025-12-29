using AvailabilityService.Database.Entities;

namespace AvailabilityService.Database.UpdateModels;

public class UpdateTimeBlock
{
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public TimeBlockType? Type { get; set; }
    public string? Reason { get; set; }
}