using AvailabilityService.Database.Entities;

namespace AvailabilityService.Api.Services.Interfaces;

public interface IRecurrenceService
{
    Task<List<TimeBlock>> GenerateRecurringTimeBlocksAsync(
        RecurrencePattern pattern,
        DateTime baseStart,
        DateTime baseEnd,
        Guid masterId,
        Guid tenantId,
        TimeBlockType type,
        string? reason = null);

    Task UpdateRecurringTimeBlocksAsync(
        Guid masterId,
        RecurrencePattern newPattern,
        DateTime baseStart,
        DateTime baseEnd,
        Guid tenantId,
        TimeBlockType type,
        string? reason = null);

    Task DeleteRecurringTimeBlocksAsync(Guid masterId, Guid tenantId);
}