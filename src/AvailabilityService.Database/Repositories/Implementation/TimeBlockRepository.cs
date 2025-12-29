using Microsoft.EntityFrameworkCore;
using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Models;
using AvailabilityService.Database.Repositories.Interfaces;
using AvailabilityService.Database.UpdateModels;

namespace AvailabilityService.Database.Repositories.Implementation;

public class TimeBlockRepository(AvailabilityDbContext context) : ITimeBlockRepository
{
    public async Task<(IEnumerable<TimeBlock> TimeBlocks, int TotalCount)> GetTimeBlocksAsync(PaginationParameters parameters, Guid? tenantId = null)
    {
        var query = context.TimeBlocks
            .AsNoTracking()
            .AsQueryable();

        if (tenantId.HasValue)
        {
            query = query.Where(tb => tb.TenantId == tenantId.Value);
        }

        var totalCount = await query.CountAsync();

        var timeBlocks = await query
            .OrderBy(tb => tb.StartDateTime)
            .Skip(parameters.Offset)
            .Take(parameters.Limit)
            .ToListAsync();

        return (timeBlocks, totalCount);
    }

    public async Task<(IEnumerable<TimeBlock> TimeBlocks, int TotalCount)> GetTimeBlocksByDateRangeAsync(DateTime start, DateTime end, Guid? tenantId = null)
    {
        var query = context.TimeBlocks
            .AsNoTracking()
            .Where(tb => tb.StartDateTime >= start && tb.EndDateTime <= end);

        if (tenantId.HasValue)
        {
            query = query.Where(tb => tb.TenantId == tenantId.Value);
        }

        var totalCount = await query.CountAsync();

        var timeBlocks = await query
            .OrderBy(tb => tb.StartDateTime)
            .ToListAsync();

        return (timeBlocks, totalCount);
    }

    public async Task<TimeBlock?> GetTimeBlockByIdAsync(Guid id)
    {
        return await context.TimeBlocks
            .AsNoTracking()
            .FirstOrDefaultAsync(tb => tb.Id == id);
    }

    public async Task<IEnumerable<TimeBlock>> GetTimeBlocksByTenantAsync(Guid tenantId)
    {
        return await context.TimeBlocks
            .AsNoTracking()
            .Where(tb => tb.TenantId == tenantId)
            .OrderBy(tb => tb.StartDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<TimeBlock>> GetTimeBlocksByTenantAndDateRangeAsync(Guid tenantId, DateTime startDate, DateTime endDate)
    {
        return await context.TimeBlocks
            .AsNoTracking()
            .Where(tb => tb.TenantId == tenantId &&
                        tb.StartDateTime < endDate &&
                        tb.EndDateTime > startDate)
            .OrderBy(tb => tb.StartDateTime)
            .ToListAsync();
    }

    public async Task CreateTimeBlockAsync(TimeBlock timeBlock)
    {
        timeBlock.Id = Guid.NewGuid();

        await context.TimeBlocks.AddAsync(timeBlock);
        await context.SaveChangesAsync();
    }

    public async Task<bool> UpdateTimeBlockAsync(Guid id, UpdateTimeBlock updateRequest)
    {
        var existingTimeBlock = await context.TimeBlocks.FindAsync(id);
        if (existingTimeBlock == null) return false;

        if (updateRequest.StartDateTime.HasValue)
            existingTimeBlock.StartDateTime = updateRequest.StartDateTime.Value;

        if (updateRequest.EndDateTime.HasValue)
            existingTimeBlock.EndDateTime = updateRequest.EndDateTime.Value;

        if (updateRequest.Type.HasValue)
            existingTimeBlock.Type = updateRequest.Type.Value;

        if (updateRequest.Reason != null)
            existingTimeBlock.Reason = updateRequest.Reason;


        existingTimeBlock.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTimeBlockAsync(Guid id)
    {
        var timeBlock = await context.TimeBlocks.FindAsync(id);
        if (timeBlock == null) return false;

        context.TimeBlocks.Remove(timeBlock);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTimeBlockAsync(Guid id, Guid tenantId)
    {
        var timeBlock = await context.TimeBlocks
            .FirstOrDefaultAsync(tb => tb.Id == id && tb.TenantId == tenantId);

        if (timeBlock == null)
        {
            return false;
        }

        context.TimeBlocks.Remove(timeBlock);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<int> DeleteTimeBlocksByDateRangeAsync(DateTime start, DateTime end, Guid tenantId)
    {
        var timeBlocksToDelete = await context.TimeBlocks
            .Where(tb => tb.TenantId == tenantId &&
                        tb.StartDateTime >= start &&
                        tb.EndDateTime <= end)
            .ToListAsync();

        if (timeBlocksToDelete.Any())
        {
            context.TimeBlocks.RemoveRange(timeBlocksToDelete);
            await context.SaveChangesAsync();
        }

        return timeBlocksToDelete.Count;
    }

    public async Task<IEnumerable<TimeBlock>> GetTimeBlocksByRecurrenceIdAsync(Guid recurrenceId, Guid tenantId)
    {
        return await context.TimeBlocks
            .AsNoTracking()
            .Where(tb => tb.RecurrenceId == recurrenceId && tb.TenantId == tenantId)
            .OrderBy(tb => tb.StartDateTime)
            .ToListAsync();
    }

    public async Task CreateMultipleTimeBlocksAsync(IEnumerable<TimeBlock> timeBlocks)
    {
        var blocks = timeBlocks.ToList();

        await context.TimeBlocks.AddRangeAsync(blocks);
        await context.SaveChangesAsync();
    }

    public async Task DeleteMultipleTimeBlocksAsync(IEnumerable<Guid> ids)
    {
        var idsList = ids.ToList();
        if (!idsList.Any()) return;

        var timeBlocksToDelete = await context.TimeBlocks
            .Where(tb => idsList.Contains(tb.Id))
            .ToListAsync();

        if (timeBlocksToDelete.Any())
        {
            context.TimeBlocks.RemoveRange(timeBlocksToDelete);
            await context.SaveChangesAsync();
        }
    }
}