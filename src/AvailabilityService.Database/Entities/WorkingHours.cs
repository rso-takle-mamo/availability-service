using System;

namespace AvailabilityService.Database.Entities;

public class WorkingHours
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int MaxConcurrentBookings { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}