using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Models;
using AvailabilityService.Database.UpdateModels;

namespace AvailabilityService.Database.Repositories.Interfaces;

public interface IWorkingHoursRepository
{
    Task<WorkingHours?> GetWorkingHoursByIdAsync(Guid id);
    Task<WorkingHours?> GetWorkingHoursByTenantAndDayAsync(Guid tenantId, DayOfWeek day);
    Task<IEnumerable<WorkingHours>> GetWorkingHoursByTenantAsync(Guid tenantId);
    Task CreateWorkingHoursAsync(WorkingHours workingHours);
    Task<bool> UpdateWorkingHoursAsync(Guid id, UpdateWorkingHours updateRequest);
    Task<bool> DeleteWorkingHoursAsync(Guid id);
    Task<bool> DeleteWorkingHoursAsync(Guid id, Guid tenantId);
    Task<bool> DeleteWorkingHoursByTenantAsync(Guid tenantId);
    Task<int> CreateMultipleWorkingHoursAsync(IEnumerable<WorkingHours> workingHours);
}