using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Models;
using AvailabilityService.Database.UpdateModels;

namespace AvailabilityService.Database.Repositories.Interfaces;

public interface ITimeBlockRepository
{
    Task<(IEnumerable<TimeBlock> TimeBlocks, int TotalCount)> GetTimeBlocksAsync(PaginationParameters parameters, Guid? tenantId = null);
    Task<(IEnumerable<TimeBlock> TimeBlocks, int TotalCount)> GetTimeBlocksByDateRangeAsync(DateTime start, DateTime end, Guid? tenantId = null);
    Task<TimeBlock?> GetTimeBlockByIdAsync(Guid id);
    Task<IEnumerable<TimeBlock>> GetTimeBlocksByTenantAsync(Guid tenantId);
    Task<IEnumerable<TimeBlock>> GetTimeBlocksByTenantAndDateRangeAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<TimeBlock>> GetTimeBlocksByRecurrenceIdAsync(Guid recurrenceId, Guid tenantId);
    Task CreateTimeBlockAsync(TimeBlock timeBlock);
    Task CreateMultipleTimeBlocksAsync(IEnumerable<TimeBlock> timeBlocks);
    Task<bool> UpdateTimeBlockAsync(Guid id, UpdateTimeBlock updateRequest);
    Task<bool> DeleteTimeBlockAsync(Guid id, Guid tenantId);
    Task<int> DeleteTimeBlocksByDateRangeAsync(DateTime start, DateTime end, Guid tenantId);
    Task DeleteMultipleTimeBlocksAsync(IEnumerable<Guid> ids);
}