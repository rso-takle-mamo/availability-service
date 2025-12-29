using AvailabilityService.Api.Responses;
using AvailabilityService.Api.Models.Internal;

namespace AvailabilityService.Api.Services.Interfaces;

public interface IAvailabilityService
{
    Task<AvailableTimeRangeResponse> GetAvailableRangesAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    Task<AvailabilityCheckResult> IsTimeSlotAvailableAsync(Guid tenantId, Guid serviceId, DateTime startTime, DateTime endTime);
    Task<List<ConflictInfo>> DetectAllConflictsAsync(Guid tenantId, DateTime startTime, DateTime endTime, int bufferBeforeMinutes, int bufferAfterMinutes);
}