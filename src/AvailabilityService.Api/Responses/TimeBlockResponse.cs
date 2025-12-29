using AvailabilityService.Database.Entities;

namespace AvailabilityService.Api.Responses;

/// <summary>
/// Response model for time blocks (unavailable periods)
/// </summary>
public class TimeBlockResponse
{
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <example>456e7890-e89b-12d3-a456-426614174001</example>
    public Guid TenantId { get; set; }

    /// <example>2025-12-25T09:00:00Z</example>
    public DateTime StartDateTime { get; set; }

    /// <example>2025-12-25T17:00:00Z</example>
    public DateTime EndDateTime { get; set; }

    /// <example>Vacation</example>
    public TimeBlockType Type { get; set; }

    /// <example>Christmas holidays</example>
    public string? Reason { get; set; }

    /// <example>false</example>
    public bool IsRecurring { get; set; }

    /// <example>2025-12-09T10:00:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <example>2025-12-09T11:00:00Z</example>
    public DateTime UpdatedAt { get; set; }
}