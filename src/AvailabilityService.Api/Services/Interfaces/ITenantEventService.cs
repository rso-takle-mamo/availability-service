using AvailabilityService.Api.Events.Tenant;

namespace AvailabilityService.Api.Services.Interfaces;

public interface ITenantEventService
{
    Task HandleTenantCreatedEventAsync(TenantCreatedEvent tenantEvent);
    Task HandleTenantUpdatedEventAsync(TenantUpdatedEvent tenantEvent);
}
