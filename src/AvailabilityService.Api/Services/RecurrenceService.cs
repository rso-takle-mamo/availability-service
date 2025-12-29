using AvailabilityService.Api.Services.Interfaces;
using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Repositories.Interfaces;

namespace AvailabilityService.Api.Services;

public class RecurrenceService(ITimeBlockRepository timeBlockRepository, ILogger<RecurrenceService> logger)
    : IRecurrenceService
{
    private readonly TimeSpan _defaultFutureWindow = TimeSpan.FromDays(365 * 2); // 2 years

    public async Task<List<TimeBlock>> GenerateRecurringTimeBlocksAsync(
        RecurrencePattern pattern,
        DateTime baseStart,
        DateTime baseEnd,
        Guid masterId,
        Guid tenantId,
        TimeBlockType type,
        string? reason = null)
    {
        var timeBlocks = new List<TimeBlock>();
        var endDate = GetEffectiveEndDate(pattern, baseStart);
        var duration = baseEnd - baseStart;
        var totalOccurrences = 0;

        logger.LogInformation("Generating recurring blocks - BaseStart: {BaseStart}, EndDate: {EndDate}, Duration: {Duration}",
            baseStart, endDate, duration);

        var adjustedMaxOccurrences = pattern.MaxOccurrences - 1;

        if (pattern.HasDaysOfWeek)
        {
            // Weekly: Generate occurrences across all specified days
            var allDates = new List<DateTime>();

            var masterDayOfWeek = baseStart.DayOfWeek;
            foreach (var day in pattern.DaysOfWeek)
            {
                var targetDay = day;
                
                // Only process days that come after the master timeblock's day in the same week
                if (targetDay > masterDayOfWeek)
                {
                    var daysUntilTarget = (double)(day - (int)masterDayOfWeek);
                    var targetDate = baseStart.Date.AddDays(daysUntilTarget);
                    var targetDateTime = targetDate.Add(baseStart.TimeOfDay);

                    // Ensure we're within the date range
                    if (targetDateTime <= endDate)
                    {
                        allDates.Add(targetDateTime);
                        totalOccurrences++;

                        // Stop if we've reached max occurrences
                        if (adjustedMaxOccurrences.HasValue && totalOccurrences >= adjustedMaxOccurrences.Value)
                            break;
                    }
                }
            }

            // If we haven't reached max occurrences, continue with weekly generation
            if (!adjustedMaxOccurrences.HasValue || totalOccurrences < adjustedMaxOccurrences.Value)
            {
                // Start from the next week
                var nextWeekStart = baseStart.Date.AddDays(7 - (int)masterDayOfWeek).Add(baseStart.TimeOfDay);

                while (nextWeekStart <= endDate && (!adjustedMaxOccurrences.HasValue || totalOccurrences < adjustedMaxOccurrences.Value))
                {
                    foreach (var day in pattern.DaysOfWeek)
                    {
                        var targetDay = (DayOfWeek)day;
                        var weekStart = nextWeekStart.Date.AddDays(-((int)nextWeekStart.DayOfWeek));
                        var dateForWeek = weekStart.AddDays((int)targetDay);
                        var targetDateTime = dateForWeek.Add(nextWeekStart.TimeOfDay);

                        // Ensure we're within the date range
                        if (targetDateTime <= endDate)
                        {
                            allDates.Add(targetDateTime);
                            totalOccurrences++;

                            // Stop if we've reached max occurrences
                            if (adjustedMaxOccurrences.HasValue && totalOccurrences >= adjustedMaxOccurrences.Value)
                                break;
                        }
                    }

                    if (adjustedMaxOccurrences.HasValue && totalOccurrences >= adjustedMaxOccurrences.Value)
                        break;

                    nextWeekStart = nextWeekStart.AddDays(7 * pattern.Interval);
                }
            }

            foreach (var date in allDates.OrderBy(d => d))
            {
                var timeBlock = CreateTimeBlockInstance(date, date + duration, masterId, tenantId, type, reason);
                timeBlocks.Add(timeBlock);
            }
        }
        else if (pattern.HasDaysOfMonth)
        {
            // Monthly: Generate occurrences across all specified days
            var allDates = new List<DateTime>();
            var currentStart = baseStart;

            while (currentStart <= endDate && (!adjustedMaxOccurrences.HasValue || totalOccurrences < adjustedMaxOccurrences.Value))
            {
                foreach (var day in pattern.DaysOfMonth)
                {
                    var dayStart = CalculateMonthlyOccurrence(currentStart, day);

                    if (dayStart >= baseStart && dayStart <= endDate)
                    {
                        // Skip if this is the master timeblock
                        if (!(dayStart.Date == baseStart.Date && dayStart.TimeOfDay == baseStart.TimeOfDay))
                        {
                            allDates.Add(dayStart);
                            totalOccurrences++;
                            logger.LogInformation("Added recurring timeblock: {DayStart} from currentStart {CurrentStart}",
                                dayStart, currentStart);

                            // Stop if we've reached max occurrences
                            if (adjustedMaxOccurrences.HasValue && totalOccurrences >= adjustedMaxOccurrences.Value)
                                break;
                        }
                        else
                        {
                            logger.LogDebug("Skipping master timeblock: {DayStart}", dayStart);
                        }
                    }
                    else
                    {
                        logger.LogWarning("Timeblock {DayStart} is outside range. DayStart >= BaseStart: {Condition1}, DayStart <= EndDate: {Condition2}",
                            dayStart, dayStart >= baseStart, dayStart <= endDate);
                    }
                }

                if (adjustedMaxOccurrences.HasValue && totalOccurrences >= adjustedMaxOccurrences.Value)
                    break;

                var nextStart = currentStart.AddMonths(pattern.Interval);
                logger.LogDebug("Moving currentStart from {CurrentStart} to {NextStart} (endDate: {EndDate})",
                    currentStart, nextStart, endDate);
                currentStart = nextStart;
            }

            foreach (var date in allDates.OrderBy(d => d))
            {
                var timeBlock = CreateTimeBlockInstance(date, date + duration, masterId, tenantId, type, reason);
                timeBlocks.Add(timeBlock);
            }
        }
        else
        {
            // Daily: Generate occurrences, skipping the master timeblock
            var currentStart = baseStart.AddDays(pattern.Interval);

            while (currentStart <= endDate && (!adjustedMaxOccurrences.HasValue || totalOccurrences < adjustedMaxOccurrences.Value))
            {
                var timeBlock = CreateTimeBlockInstance(currentStart, currentStart + duration, masterId, tenantId, type, reason);
                timeBlocks.Add(timeBlock);
                totalOccurrences++;
                currentStart = currentStart.AddDays(pattern.Interval);
            }
        }

        if (timeBlocks.Count != 0)
        {
            await timeBlockRepository.CreateMultipleTimeBlocksAsync(timeBlocks);
            logger.LogInformation("Generated {Count} recurring time blocks for master {MasterId}", timeBlocks.Count, masterId);
        }

        return timeBlocks;
    }

    private DateTime CalculateMonthlyOccurrence(DateTime baseDate, int day)
    {
        if (day > 0)
        {
            // Positive day: specific day of month
            var daysInMonth = DateTime.DaysInMonth(baseDate.Year, baseDate.Month);
            return DateTime.SpecifyKind(new DateTime(
                baseDate.Year,
                baseDate.Month,
                Math.Min(day, daysInMonth),
                baseDate.Hour,
                baseDate.Minute,
                baseDate.Second), DateTimeKind.Utc);
        }
        else
        {
            // Negative day: count from end of month
            var daysInMonth = DateTime.DaysInMonth(baseDate.Year, baseDate.Month);
            return DateTime.SpecifyKind(new DateTime(
                baseDate.Year,
                baseDate.Month,
                daysInMonth + day + 1,
                baseDate.Hour,
                baseDate.Minute,
                baseDate.Second), DateTimeKind.Utc);
        }
    }

    public async Task UpdateRecurringTimeBlocksAsync(
        Guid masterId,
        RecurrencePattern newPattern,
        DateTime baseStart,
        DateTime baseEnd,
        Guid tenantId,
        TimeBlockType type,
        string? reason = null)
    {
        await DeleteRecurringTimeBlocksAsync(masterId, tenantId);
        await GenerateRecurringTimeBlocksAsync(newPattern, baseStart, baseEnd, masterId, tenantId, type, reason);
    }

    public async Task DeleteRecurringTimeBlocksAsync(Guid masterId, Guid tenantId)
    {
        var existingBlocks = await timeBlockRepository.GetTimeBlocksByRecurrenceIdAsync(masterId, tenantId);

        if (existingBlocks.Any())
        {
            await timeBlockRepository.DeleteMultipleTimeBlocksAsync(existingBlocks.Select(b => b.Id));
            logger.LogInformation("Deleted {Count} recurring time blocks for recurrence {MasterId}", existingBlocks.Count(), masterId);
        }
    }

    private TimeBlock CreateTimeBlockInstance(
        DateTime startDateTime,
        DateTime endDateTime,
        Guid masterId,
        Guid tenantId,
        TimeBlockType type,
        string? reason)
    {
        return new TimeBlock
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StartDateTime = startDateTime,
            EndDateTime = endDateTime,
            Type = type,
            Reason = reason,
            RecurrenceId = masterId
        };
    }

    private DateTime GetEffectiveEndDate(RecurrencePattern pattern, DateTime startDate)
    {
        if (pattern.EndDate.HasValue)
            return pattern.EndDate.Value;

        // Default to 2 years in the future
        return startDate + _defaultFutureWindow;
    }
}