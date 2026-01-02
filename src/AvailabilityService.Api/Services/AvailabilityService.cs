using AvailabilityService.Api.Responses;
using AvailabilityService.Api.Services.Interfaces;
using AvailabilityService.Api.Models.Internal;
using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Repositories.Interfaces;

namespace AvailabilityService.Api.Services;

public class AvailabilityService(
    IWorkingHoursRepository workingHoursRepository,
    ITimeBlockRepository timeBlockRepository,
    ITenantRepository tenantRepository,
    IBookingRepository bookingRepository,
    ILogger<AvailabilityService> logger)
    : IAvailabilityService
{
    public async Task<AvailableTimeRangeResponse> GetAvailableRangesAsync(Guid tenantId, DateTime startDate, DateTime endDate)
    {
        var tenant = await tenantRepository.GetTenantByIdAsync(tenantId);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant not found: {tenantId}");
        }

        var (workingHours, timeBlocks, bookings) = await GetAllAvailabilityDataAsync(tenantId, startDate, endDate);

        var workingHoursList = workingHours.ToList();
        var timeBlocksList = timeBlocks.ToList();
        var bookingsList = bookings.ToList();

        var response = new AvailableTimeRangeResponse();

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var dayOfWeek = date.DayOfWeek;

            var dayWorkingHours = workingHoursList.FirstOrDefault(wh => wh.Day == dayOfWeek);

            // If no working hours defined, provider is available 24/7 (full day)
            DateTime workingStart, workingEnd;
            if (dayWorkingHours == null || dayWorkingHours.StartTime == dayWorkingHours.EndTime)
            {
                // 24/7 availability for this day
                workingStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
                workingEnd = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, DateTimeKind.Utc);
            }
            else
            {
                workingStart = new DateTime(date.Year, date.Month, date.Day,
                    dayWorkingHours.StartTime.Hour, dayWorkingHours.StartTime.Minute, 0, DateTimeKind.Utc);
                workingEnd = new DateTime(date.Year, date.Month, date.Day,
                    dayWorkingHours.EndTime.Hour, dayWorkingHours.EndTime.Minute, 0, DateTimeKind.Utc);
            }

            // Collect time blocks for this day
            var timeBlockPeriods = new List<(DateTime Start, DateTime End)>();
            foreach (var timeBlock in timeBlocksList)
            {
                if (timeBlock.StartDateTime.Date == date.Date)
                {
                    timeBlockPeriods.Add((timeBlock.StartDateTime, timeBlock.EndDateTime));
                }
            }

            // Collect bookings for this day
            var bookingPeriods = new List<(DateTime Start, DateTime End)>();
            foreach (var booking in bookingsList)
            {
                if ((booking.BookingStatus == BookingStatus.Confirmed || booking.BookingStatus == BookingStatus.Pending)
                    && booking.StartDateTime.Date == date.Date)
                {
                    bookingPeriods.Add((booking.StartDateTime, booking.EndDateTime));
                }
            }

            // Apply buffer times to bookings
            var bookingPeriodsWithBuffers = new List<(DateTime Start, DateTime End)>();
            foreach (var bookingPeriod in bookingPeriods)
            {
                bookingPeriodsWithBuffers.Add((
                    bookingPeriod.Start.AddMinutes(-tenant.BufferBeforeMinutes),
                    bookingPeriod.End.AddMinutes(tenant.BufferAfterMinutes)
                ));
            }

            var busyPeriods = timeBlockPeriods.Concat(bookingPeriodsWithBuffers).ToList();

            var availableRanges = new List<(DateTime Start, DateTime End)> { (workingStart, workingEnd) };
            foreach (var (beforeEnd, afterStart) in busyPeriods)
            {
                var newAvailableRanges = new List<(DateTime Start, DateTime End)>();
                foreach (var availableRange in availableRanges)
                {
                    if (!(availableRange.Start < afterStart && availableRange.End > beforeEnd))
                    {
                        newAvailableRanges.Add(availableRange);
                        continue;
                    }

                    if (availableRange.Start < beforeEnd)
                    {
                        if (availableRange.Start < beforeEnd)
                        {
                            newAvailableRanges.Add((availableRange.Start, beforeEnd));
                        }
                    }

                    if (availableRange.End <= afterStart) continue;

                    if (afterStart < availableRange.End)
                    {
                        newAvailableRanges.Add((afterStart, availableRange.End));
                    }
                }
                availableRanges = newAvailableRanges;
            }

            // Merge overlapping ranges
            var mergedRanges = availableRanges;
            if (mergedRanges.Count != 0)
            {
                var sortedRanges = mergedRanges.OrderBy(r => r.Start).ToList();
                var merged = new List<(DateTime Start, DateTime End)>();
                var current = sortedRanges[0];

                for (var i = 1; i < sortedRanges.Count; i++)
                {
                    var next = sortedRanges[i];
                    
                    if ((current.Start < next.End && current.End > next.Start) || current.End == next.Start)
                    {
                        current = (
                            current.Start < next.Start ? current.Start : next.Start,
                            current.End > next.End ? current.End : next.End
                        );
                    }
                    else
                    {
                        merged.Add(current);
                        current = next;
                    }
                }
                merged.Add(current);
                mergedRanges = merged;
            }

            foreach (var range in mergedRanges)
            {
                response.AvailableRanges.Add(new AvailableTimeRange
                {
                    Start = range.Start,
                    End = range.End
                });
            }
        }

        response.AvailableRanges = response.AvailableRanges.OrderBy(r => r.Start).ToList();

        return response;
    }

    public async Task<AvailabilityCheckResult> IsTimeSlotAvailableAsync(Guid tenantId, Guid serviceId, DateTime startTime, DateTime endTime)
    {
        var tenant = await tenantRepository.GetTenantByIdAsync(tenantId);
        if (tenant == null)
        {
            return new AvailabilityCheckResult
            {
                IsAvailable = false,
                Conflicts =
                [
                    new ConflictInfo
                    {
                        Type = ConflictType.WorkingHours,
                        OverlapStart = startTime,
                        OverlapEnd = endTime
                    }
                ]
            };
        }

        var conflicts = await DetectAllConflictsAsync(
            tenantId,
            startTime,
            endTime,
            tenant.BufferBeforeMinutes,
            tenant.BufferAfterMinutes);

        return new AvailabilityCheckResult
        {
            IsAvailable = conflicts.Count == 0,
            Conflicts = conflicts
        };
    }

    private static (DateTime overlapStart, DateTime overlapEnd)? CalculateOverlap(
        DateTime requestedStart,
        DateTime requestedEnd,
        DateTime conflictStart,
        DateTime conflictEnd)
    {
        if (requestedStart > conflictEnd || requestedEnd < conflictStart)
        {
            return null;
        }

        var overlapStart = requestedStart > conflictStart ? requestedStart : conflictStart;
        var overlapEnd = requestedEnd < conflictEnd ? requestedEnd : conflictEnd;

        if (overlapStart < overlapEnd)
        {
            return (overlapStart, overlapEnd);
        }

        return null;
    }
    

    private async Task<(IEnumerable<WorkingHours> WorkingHours, IEnumerable<TimeBlock> TimeBlocks, IEnumerable<Booking> Bookings)>
        GetAllAvailabilityDataAsync(Guid tenantId, DateTime startDate, DateTime endDate)
    {
        var workingHours = await workingHoursRepository.GetWorkingHoursByTenantAsync(tenantId);
        var timeBlocks = await timeBlockRepository.GetTimeBlocksByTenantAndDateRangeAsync(tenantId, startDate, endDate);
        var bookings = await bookingRepository.GetBookingsByTenantAndDateRangeAsync(tenantId, startDate, endDate);

        return (workingHours, timeBlocks, bookings);
    }
    
    
    public async Task<List<ConflictInfo>> DetectAllConflictsAsync(
        Guid tenantId,
        DateTime startTime,
        DateTime endTime,
        int bufferBeforeMinutes,
        int bufferAfterMinutes)
    {
        var fullRangeStart = startTime.AddMinutes(-bufferBeforeMinutes);
        var fullRangeEnd = endTime.AddMinutes(bufferAfterMinutes);
        var conflicts = new List<ConflictInfo>();

        var workingHours = await workingHoursRepository.GetWorkingHoursByTenantAsync(tenantId);
        var workingHoursForDays = workingHours.Where(wh => wh.Day == startTime.DayOfWeek).ToList();

        // If no working hours are defined, provider is available 24/7
        // Only check conflicts if working hours are explicitly set
        if (workingHoursForDays.Count > 0)
        {
            foreach (var dayWorkingHours in workingHoursForDays)
            {
                var dayWorkingStart = new DateTime(startTime.Year, startTime.Month, startTime.Day,
                    dayWorkingHours.StartTime.Hour, dayWorkingHours.StartTime.Minute, 0, DateTimeKind.Utc);
                var dayWorkingEnd = new DateTime(startTime.Year, startTime.Month, startTime.Day,
                    dayWorkingHours.EndTime.Hour, dayWorkingHours.EndTime.Minute, 0, DateTimeKind.Utc);

                if (startTime < dayWorkingStart)
                {
                    conflicts.Add(new ConflictInfo
                    {
                        Type = ConflictType.WorkingHours,
                        OverlapStart = startTime,
                        OverlapEnd = dayWorkingStart
                    });
                }

                if (endTime > dayWorkingEnd)
                {
                    conflicts.Add(new ConflictInfo
                    {
                        Type = ConflictType.WorkingHours,
                        OverlapStart = dayWorkingEnd,
                        OverlapEnd = endTime
                    });
                }
            }
        }

        // Check time blocks
        var timeBlocks = await timeBlockRepository.GetTimeBlocksByTenantAndDateRangeAsync(tenantId, fullRangeStart, fullRangeEnd);
        foreach (var timeBlock in timeBlocks)
        {
            var overlap = CalculateOverlap(fullRangeStart, fullRangeEnd, timeBlock.StartDateTime, timeBlock.EndDateTime);
            if (overlap.HasValue)
            {
                conflicts.Add(new ConflictInfo
                {
                    Type = ConflictType.TimeBlock,
                    OverlapStart = overlap.Value.overlapStart,
                    OverlapEnd = overlap.Value.overlapEnd
                });
            }
        }

        // Check existing bookings with their buffers
        var bookings = await bookingRepository.GetBookingsByTenantAndDateRangeAsync(tenantId, fullRangeStart, fullRangeEnd);
        foreach (var booking in bookings)
        {
            if (booking.BookingStatus == BookingStatus.Cancelled)
                continue;

            var reservedStart = booking.StartDateTime.AddMinutes(-bufferBeforeMinutes);
            var reservedEnd = booking.EndDateTime.AddMinutes(bufferAfterMinutes);

            var overlap = CalculateOverlap(fullRangeStart, fullRangeEnd, reservedStart, reservedEnd);
            if (overlap.HasValue)
            {
                conflicts.Add(new ConflictInfo
                {
                    Type = ConflictType.BufferTime,
                    OverlapStart = overlap.Value.overlapStart,
                    OverlapEnd = overlap.Value.overlapEnd
                });
            }
        }

        return conflicts;
    }
}