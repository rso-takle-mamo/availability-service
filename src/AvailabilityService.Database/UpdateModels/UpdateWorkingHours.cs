namespace AvailabilityService.Database.UpdateModels;

public class UpdateWorkingHours
{
    public DayOfWeek? Day { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? MaxConcurrentBookings { get; set; }
}