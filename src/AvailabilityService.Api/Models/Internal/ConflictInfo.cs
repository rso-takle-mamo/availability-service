namespace AvailabilityService.Api.Models.Internal;

/// <summary>
/// Internal class to represent conflict information for availability checking
/// This is NOT exposed via gRPC - used only for logging and internal processing
/// </summary>
public class ConflictInfo
{
    public ConflictType Type { get; set; }
    public DateTime OverlapStart { get; set; }
    public DateTime OverlapEnd { get; set; }
    public double OverlapMinutes => (OverlapEnd - OverlapStart).TotalMinutes;
    public string Description => $"{Type}: {OverlapStart:HH:mm} - {OverlapEnd:HH:mm} ({OverlapMinutes:F0} min overlap)";
}

/// <summary>
/// Types of conflicts that can make a time slot unavailable
/// </summary>
public enum ConflictType
{
    TimeBlock,
    WorkingHours,
    Booking,
    BufferTime
}