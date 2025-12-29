using Microsoft.EntityFrameworkCore;
using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Models;
using AvailabilityService.Database.Repositories.Interfaces;
using AvailabilityService.Database.UpdateModels;

namespace AvailabilityService.Database.Repositories.Implementation;

public class WorkingHoursRepository(AvailabilityDbContext context) : IWorkingHoursRepository
{
    public async Task<WorkingHours?> GetWorkingHoursByIdAsync(Guid id)
    {
        return await context.WorkingHours
            .AsNoTracking()
            .FirstOrDefaultAsync(wh => wh.Id == id);
    }

    public async Task CreateWorkingHoursAsync(WorkingHours workingHours)
    {
        workingHours.Id = Guid.NewGuid();
        workingHours.CreatedAt = DateTime.UtcNow;
        workingHours.UpdatedAt = DateTime.UtcNow;

        await context.WorkingHours.AddAsync(workingHours);
        await context.SaveChangesAsync();
    }

    public async Task<bool> UpdateWorkingHoursAsync(Guid id, UpdateWorkingHours updateRequest)
    {
        var existingWorkingHours = await context.WorkingHours.FindAsync(id);
        if (existingWorkingHours == null) return false;

        if (updateRequest.Day.HasValue)
            existingWorkingHours.Day = updateRequest.Day.Value;

        if (updateRequest.StartTime.HasValue)
            existingWorkingHours.StartTime = updateRequest.StartTime.Value;

        if (updateRequest.EndTime.HasValue)
            existingWorkingHours.EndTime = updateRequest.EndTime.Value;

  
        if (updateRequest.MaxConcurrentBookings.HasValue)
            existingWorkingHours.MaxConcurrentBookings = updateRequest.MaxConcurrentBookings.Value;

        existingWorkingHours.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<WorkingHours?> GetWorkingHoursByTenantAndDayAsync(Guid tenantId, DayOfWeek day)
    {
        return await context.WorkingHours
            .AsNoTracking()
            .FirstOrDefaultAsync(wh => wh.TenantId == tenantId && wh.Day == day);
    }

    public async Task<IEnumerable<WorkingHours>> GetWorkingHoursByTenantAsync(Guid tenantId)
    {
        return await context.WorkingHours
            .AsNoTracking()
            .Where(wh => wh.TenantId == tenantId)
            .OrderBy(wh => wh.Day)
            .ThenBy(wh => wh.StartTime)
            .ToListAsync();
    }

    public async Task<bool> DeleteWorkingHoursAsync(Guid id)
    {
        var workingHours = await context.WorkingHours.FindAsync(id);
        if (workingHours == null) return false;

        context.WorkingHours.Remove(workingHours);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWorkingHoursAsync(Guid id, Guid tenantId)
    {
        var workingHours = await context.WorkingHours
            .FirstOrDefaultAsync(wh => wh.Id == id && wh.TenantId == tenantId);

        if (workingHours == null)
        {
            return false;
        }

        context.WorkingHours.Remove(workingHours);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWorkingHoursByTenantAsync(Guid tenantId)
    {
        var workingHoursToDelete = await context.WorkingHours
            .Where(wh => wh.TenantId == tenantId)
            .ToListAsync();

        if (workingHoursToDelete.Any())
        {
            context.WorkingHours.RemoveRange(workingHoursToDelete);
            await context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<int> CreateMultipleWorkingHoursAsync(IEnumerable<WorkingHours> workingHours)
    {
        var workingHoursList = workingHours.ToList();

        foreach (var wh in workingHoursList)
        {
            wh.CreatedAt = DateTime.UtcNow;
            wh.UpdatedAt = DateTime.UtcNow;
        }

        await context.WorkingHours.AddRangeAsync(workingHoursList);
        await context.SaveChangesAsync();

        return workingHoursList.Count;
    }
}