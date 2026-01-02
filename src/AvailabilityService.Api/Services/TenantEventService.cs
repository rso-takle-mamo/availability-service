using Microsoft.Extensions.Logging;
using AvailabilityService.Api.Events.Tenant;
using AvailabilityService.Api.Services.Interfaces;
using AvailabilityService.Database.Repositories.Interfaces;

namespace AvailabilityService.Api.Services;

public class TenantEventService(
    ILogger<TenantEventService> logger,
    ITenantRepository tenantRepository) : ITenantEventService
{
    public async Task HandleTenantCreatedEventAsync(TenantCreatedEvent tenantEvent)
    {
        logger.LogInformation("Handling tenant created event for tenant ID: {TenantId}", tenantEvent.TenantId);

        try
        {
            var existingTenant = await tenantRepository.GetTenantByIdAsync(tenantEvent.TenantId);
            if (existingTenant != null)
            {
                logger.LogWarning("Tenant with ID {TenantId} already exists, skipping creation", tenantEvent.TenantId);
                return;
            }

            var tenant = new Database.Entities.Tenant
            {
                Id = tenantEvent.TenantId,
                BusinessName = tenantEvent.BusinessName,
                Email = tenantEvent.BusinessEmail,
                Phone = tenantEvent.BusinessPhone,
                Address = tenantEvent.Address,
                TimeZone = null,
                BufferBeforeMinutes = 0,
                BufferAfterMinutes = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await tenantRepository.CreateTenantAsync(tenant);
            logger.LogInformation("Successfully created tenant {TenantId} in availability database", tenantEvent.TenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling tenant created event for tenant ID: {TenantId}", tenantEvent.TenantId);
            throw;
        }
    }

    public async Task HandleTenantUpdatedEventAsync(TenantUpdatedEvent tenantEvent)
    {
        logger.LogInformation("Handling tenant updated event for tenant ID: {TenantId}", tenantEvent.TenantId);

        try
        {
            var existingTenant = await tenantRepository.GetTenantByIdAsync(tenantEvent.TenantId);
            if (existingTenant == null)
            {
                logger.LogWarning("Tenant with ID {TenantId} not found for update", tenantEvent.TenantId);
                return;
            }

            var updatedTenant = new Database.Entities.Tenant
            {
                Id = tenantEvent.TenantId,
                BusinessName = tenantEvent.BusinessName,
                Email = tenantEvent.BusinessEmail,
                Phone = tenantEvent.BusinessPhone,
                Address = tenantEvent.Address,
                TimeZone = existingTenant.TimeZone,
                BufferBeforeMinutes = existingTenant.BufferBeforeMinutes,
                BufferAfterMinutes = existingTenant.BufferAfterMinutes,
                CreatedAt = existingTenant.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            await tenantRepository.UpdateTenantAsync(updatedTenant);
            logger.LogInformation("Successfully updated tenant {TenantId} in availability database", tenantEvent.TenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling tenant updated event for tenant ID: {TenantId}", tenantEvent.TenantId);
            throw;
        }
    }
}
