using System.Text.Json.Serialization;

namespace AvailabilityService.Database.Entities;

public class RecurrencePattern
{
    public RecurrenceFrequency Frequency { get; set; }
    public int Interval { get; set; } = 1; // Every 1 day, 2 weeks, etc.

    // For weekly patterns
    public DayOfWeek[]? DaysOfWeek { get; set; }

    // For monthly patterns - supports multiple days and special values like -1 -last day of he month
    public int[] DaysOfMonth { get; set; } = [];

    // End condition (at least one should be specified)
    public DateTime? EndDate { get; set; }
    public int? MaxOccurrences { get; set; }

    public bool HasDaysOfWeek => DaysOfWeek != null && DaysOfWeek.Length > 0;
    public bool HasDaysOfMonth => DaysOfMonth != null && DaysOfMonth.Length > 0;
    
    
    public static RecurrencePattern Daily(int interval = 1, DateTime? endDate = null, int? maxOccurrences = null)
        => new() { Frequency = RecurrenceFrequency.Daily, Interval = interval, EndDate = endDate, MaxOccurrences = maxOccurrences };
    
    public static RecurrencePattern Weekly(DayOfWeek[] daysOfWeek, int interval = 1, DateTime? endDate = null, int? maxOccurrences = null)
        => new() { Frequency = RecurrenceFrequency.Weekly, Interval = interval, DaysOfWeek = daysOfWeek, EndDate = endDate, MaxOccurrences = maxOccurrences };
    
    public static RecurrencePattern Weekdays(int interval = 1, DateTime? endDate = null, int? maxOccurrences = null)
        => new() { Frequency = RecurrenceFrequency.Weekly, Interval = interval, DaysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, EndDate = endDate, MaxOccurrences = maxOccurrences };
    
    public static RecurrencePattern Weekends(int interval = 1, DateTime? endDate = null, int? maxOccurrences = null)
        => new() { Frequency = RecurrenceFrequency.Weekly, Interval = interval, DaysOfWeek = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }, EndDate = endDate, MaxOccurrences = maxOccurrences };
    
    public static RecurrencePattern Monthly(int[] daysOfMonth, int interval = 1, DateTime? endDate = null, int? maxOccurrences = null)
        => new() { Frequency = RecurrenceFrequency.Monthly, Interval = interval, DaysOfMonth = daysOfMonth, EndDate = endDate, MaxOccurrences = maxOccurrences };
    
    public static RecurrencePattern Monthly(int dayOfMonth, int interval = 1, DateTime? endDate = null, int? maxOccurrences = null)
        => new() { Frequency = RecurrenceFrequency.Monthly, Interval = interval, DaysOfMonth = new[] { dayOfMonth }, EndDate = endDate, MaxOccurrences = maxOccurrences };
}

public enum RecurrenceFrequency
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}