namespace AvailabilityService.Api.Responses;

/// <summary>
/// Response model for working hours
/// </summary>
public class WorkingHoursResponse
{
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <example>456e7890-e89b-12d3-a456-426614174001</example>
    public Guid TenantId { get; set; }

    /// <example>1</example>
    public DayOfWeek Day { get; set; }

    /// <example>09:00:00</example>
    public TimeOnly StartTime { get; set; }

    /// <example>17:00:00</example>
    public TimeOnly EndTime { get; set; }

    /// <example>1</example>
    public int MaxConcurrentBookings { get; set; }

    /// <example>2025-12-09T10:00:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <example>2025-12-09T11:00:00Z</example>
    public DateTime UpdatedAt { get; set; }
}