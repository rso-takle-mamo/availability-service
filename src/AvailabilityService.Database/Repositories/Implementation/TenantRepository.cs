using Microsoft.EntityFrameworkCore;
using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Repositories.Interfaces;

namespace AvailabilityService.Database.Repositories.Implementation;

public class TenantRepository(AvailabilityDbContext context) : ITenantRepository
{
    public async Task<Tenant?> GetTenantByIdAsync(Guid id)
    {
        return await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task CreateTenantAsync(Tenant tenant)
    {
        await context.Tenants.AddAsync(tenant);
        await context.SaveChangesAsync();
    }

    public async Task UpdateTenantAsync(Tenant tenant)
    {
        context.Tenants.Update(tenant);
        await context.SaveChangesAsync();
    }

    public async Task DeleteTenantAsync(Guid tenantId)
    {
        var tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant != null)
        {
            context.Tenants.Remove(tenant);
            await context.SaveChangesAsync();
        }
    }
}