using AvailabilityService.Database.Entities;

namespace AvailabilityService.Database.Repositories.Interfaces;

public interface ITenantRepository
{
    Task<Tenant?> GetTenantByIdAsync(Guid id);
    Task CreateTenantAsync(Tenant tenant);
    Task UpdateTenantAsync(Tenant tenant);
    Task DeleteTenantAsync(Guid tenantId);
}