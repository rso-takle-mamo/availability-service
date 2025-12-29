using System.Text.Json.Serialization;

namespace AvailabilityService.Database.Entities;

public enum TimeBlockType
{
    Vacation,
    Break,
    Custom,
  }

public class TimeBlock
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public TimeBlockType Type { get; set; }
    public string? Reason { get; set; }
    public Guid? RecurrenceId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }


    [JsonIgnore]
    public bool IsRecurring => RecurrenceId.HasValue;
}