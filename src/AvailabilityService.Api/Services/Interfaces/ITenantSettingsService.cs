using AvailabilityService.Api.Requests.TenantSettings;
using AvailabilityService.Api.Responses;

namespace AvailabilityService.Api.Services.Interfaces;

public interface ITenantSettingsService
{
    Task<(int BufferBeforeMinutes, int BufferAfterMinutes)> GetBufferSettingsAsync(Guid tenantId);
    Task<(int BufferBeforeMinutes, int BufferAfterMinutes)> UpdateBufferSettingsAsync(
        Guid tenantId,
        int? bufferBeforeMinutes,
        int? bufferAfterMinutes);
    Task<TenantSettingsResponse> GetTenantSettingsAsync(Guid tenantId);
    Task<TenantSettingsResponse> CreateTenantSettingsAsync(Guid tenantId, CreateTenantSettingsRequest request);
    Task<TenantSettingsResponse> UpdateTenantSettingsAsync(Guid tenantId, UpdateTenantSettingsRequest request);
    Task<TenantSettingsResponse> PatchTenantSettingsAsync(Guid tenantId, PatchTenantSettingsRequest request);
    Task ResetBufferSettingsAsync(Guid tenantId);
}