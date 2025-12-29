using AvailabilityService.Api.Requests.TenantSettings;
using AvailabilityService.Api.Responses;
using AvailabilityService.Api.Services.Interfaces;
using AvailabilityService.Database;
using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AvailabilityService.Api.Services;

public class TenantSettingsService(
    ITenantRepository tenantRepository,
    AvailabilityDbContext context,
    ILogger<TenantSettingsService> logger)
    : ITenantSettingsService
{
    public async Task<(int BufferBeforeMinutes, int BufferAfterMinutes)> GetBufferSettingsAsync(Guid tenantId)
    {
        var tenant = await tenantRepository.GetTenantByIdAsync(tenantId);
        return tenant == null ? throw new KeyNotFoundException($"Tenant not found: {tenantId}") : (tenant.BufferBeforeMinutes, tenant.BufferAfterMinutes);
    }

    public async Task<(int BufferBeforeMinutes, int BufferAfterMinutes)> UpdateBufferSettingsAsync(
        Guid tenantId,
        int? bufferBeforeMinutes,
        int? bufferAfterMinutes)
    {
        if (bufferBeforeMinutes.HasValue)
        {
            switch (bufferBeforeMinutes.Value)
            {
                case < 0:
                    throw new ArgumentException("BufferBeforeMinutes cannot be negative");
                case > 480:
                    throw new ArgumentException("BufferBeforeMinutes cannot exceed 480 minutes (8 hours)");
            }
        }

        if (bufferAfterMinutes.HasValue)
        {
            if (bufferAfterMinutes.Value < 0)
                throw new ArgumentException("BufferAfterMinutes cannot be negative");
            if (bufferAfterMinutes.Value > 480)
                throw new ArgumentException("BufferAfterMinutes cannot exceed 480 minutes (8 hours)");
        }

        if (!bufferBeforeMinutes.HasValue && !bufferAfterMinutes.HasValue)
        {
            throw new ArgumentException("At least one buffer setting must be provided");
        }

        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant not found: {tenantId}");
        }

        if (bufferBeforeMinutes.HasValue)
        {
            tenant.BufferBeforeMinutes = bufferBeforeMinutes.Value;
        }

        if (bufferAfterMinutes.HasValue)
        {
            tenant.BufferAfterMinutes = bufferAfterMinutes.Value;
        }

        tenant.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("Updated buffer settings for tenant {TenantId}: BufferBefore={Before}, BufferAfter={After}",
            tenantId, tenant.BufferBeforeMinutes, tenant.BufferAfterMinutes);

        return (tenant.BufferBeforeMinutes, tenant.BufferAfterMinutes);
    }

    public async Task<TenantSettingsResponse> GetTenantSettingsAsync(Guid tenantId)
    {
        var tenant = await tenantRepository.GetTenantByIdAsync(tenantId);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant not found: {tenantId}");
        }

        return new TenantSettingsResponse
        {
            Id = tenant.Id,
            BusinessName = tenant.BusinessName,
            Email = tenant.Email,
            Phone = tenant.Phone,
            Address = tenant.Address,
            TimeZone = tenant.TimeZone,
            BufferBeforeMinutes = tenant.BufferBeforeMinutes,
            BufferAfterMinutes = tenant.BufferAfterMinutes,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };
    }

    public async Task<TenantSettingsResponse> CreateTenantSettingsAsync(Guid tenantId, CreateTenantSettingsRequest request)
    {
        var existingTenant = await tenantRepository.GetTenantByIdAsync(tenantId);
        if (existingTenant != null)
        {
            throw new InvalidOperationException($"Tenant settings already exist for tenant: {tenantId}");
        }

        if (!string.IsNullOrEmpty(request.TimeZone))
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone);
            }
            catch
            {
                throw new ArgumentException($"Invalid time zone: {request.TimeZone}");
            }
        }

        var tenant = new Tenant
        {
            Id = tenantId,
            BusinessName = request.BusinessName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            TimeZone = request.TimeZone ?? "UTC",
            BufferBeforeMinutes = request.BufferBeforeMinutes ?? 0,
            BufferAfterMinutes = request.BufferAfterMinutes ?? 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await tenantRepository.CreateTenantAsync(tenant);

        logger.LogInformation("Created tenant settings for tenant {TenantId}: {BusinessName}",
            tenantId, request.BusinessName);

        return new TenantSettingsResponse
        {
            Id = tenant.Id,
            BusinessName = tenant.BusinessName,
            Email = tenant.Email,
            Phone = tenant.Phone,
            Address = tenant.Address,
            TimeZone = tenant.TimeZone,
            BufferBeforeMinutes = tenant.BufferBeforeMinutes,
            BufferAfterMinutes = tenant.BufferAfterMinutes,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };
    }

    public async Task<TenantSettingsResponse> UpdateTenantSettingsAsync(Guid tenantId, UpdateTenantSettingsRequest request)
    {
        var tenant = await tenantRepository.GetTenantByIdAsync(tenantId);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant not found: {tenantId}");
        }

        if (request.BufferBeforeMinutes < 0)
            throw new ArgumentException("BufferBeforeMinutes cannot be negative");
        if (request.BufferAfterMinutes < 0)
            throw new ArgumentException("BufferAfterMinutes cannot be negative");
        if (request.BufferBeforeMinutes > 480)
            throw new ArgumentException("BufferBeforeMinutes cannot exceed 480 minutes (8 hours)");
        if (request.BufferAfterMinutes > 480)
            throw new ArgumentException("BufferAfterMinutes cannot exceed 480 minutes (8 hours)");

        if (!string.IsNullOrEmpty(request.TimeZone))
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone);
            }
            catch
            {
                throw new ArgumentException($"Invalid time zone: {request.TimeZone}");
            }
        }

        tenant.BusinessName = request.BusinessName;
        tenant.Email = request.Email;
        tenant.Phone = request.Phone;
        tenant.Address = request.Address;
        tenant.TimeZone = request.TimeZone;
        tenant.BufferBeforeMinutes = request.BufferBeforeMinutes;
        tenant.BufferAfterMinutes = request.BufferAfterMinutes;
        tenant.UpdatedAt = DateTime.UtcNow;

        await tenantRepository.UpdateTenantAsync(tenant);

        logger.LogInformation("Updated tenant settings for tenant {TenantId}: {BusinessName}",
            tenantId, request.BusinessName);

        return new TenantSettingsResponse
        {
            Id = tenant.Id,
            BusinessName = tenant.BusinessName,
            Email = tenant.Email,
            Phone = tenant.Phone,
            Address = tenant.Address,
            TimeZone = tenant.TimeZone,
            BufferBeforeMinutes = tenant.BufferBeforeMinutes,
            BufferAfterMinutes = tenant.BufferAfterMinutes,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };
    }

    public async Task<TenantSettingsResponse> PatchTenantSettingsAsync(Guid tenantId, PatchTenantSettingsRequest request)
    {
        var tenant = await tenantRepository.GetTenantByIdAsync(tenantId);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant not found: {tenantId}");
        }

        if (request.BusinessName == null &&
            request.Email == null &&
            request.Phone == null &&
            request.Address == null &&
            request.TimeZone == null &&
            !request.BufferBeforeMinutes.HasValue &&
            !request.BufferAfterMinutes.HasValue)
        {
            throw new ArgumentException("At least one property must be provided for patch");
        }

        if (request.BufferBeforeMinutes.HasValue)
        {
            if (request.BufferBeforeMinutes.Value < 0)
                throw new ArgumentException("BufferBeforeMinutes cannot be negative");
            if (request.BufferBeforeMinutes.Value > 480)
                throw new ArgumentException("BufferBeforeMinutes cannot exceed 480 minutes (8 hours)");
        }

        if (request.BufferAfterMinutes.HasValue)
        {
            if (request.BufferAfterMinutes.Value < 0)
                throw new ArgumentException("BufferAfterMinutes cannot be negative");
            if (request.BufferAfterMinutes.Value > 480)
                throw new ArgumentException("BufferAfterMinutes cannot exceed 480 minutes (8 hours)");
        }

        if (!string.IsNullOrEmpty(request.TimeZone))
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone);
            }
            catch
            {
                throw new ArgumentException($"Invalid time zone: {request.TimeZone}");
            }
        }

        if (request.BusinessName != null)
            tenant.BusinessName = request.BusinessName;
        if (request.Email != null)
            tenant.Email = request.Email;
        if (request.Phone != null)
            tenant.Phone = request.Phone;
        if (request.Address != null)
            tenant.Address = request.Address;
        if (request.TimeZone != null)
            tenant.TimeZone = request.TimeZone;
        if (request.BufferBeforeMinutes.HasValue)
            tenant.BufferBeforeMinutes = request.BufferBeforeMinutes.Value;
        if (request.BufferAfterMinutes.HasValue)
            tenant.BufferAfterMinutes = request.BufferAfterMinutes.Value;

        tenant.UpdatedAt = DateTime.UtcNow;

        await tenantRepository.UpdateTenantAsync(tenant);

        logger.LogInformation("Patched tenant settings for tenant {TenantId}", tenantId);

        return new TenantSettingsResponse
        {
            Id = tenant.Id,
            BusinessName = tenant.BusinessName,
            Email = tenant.Email,
            Phone = tenant.Phone,
            Address = tenant.Address,
            TimeZone = tenant.TimeZone,
            BufferBeforeMinutes = tenant.BufferBeforeMinutes,
            BufferAfterMinutes = tenant.BufferAfterMinutes,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };
    }

    public async Task ResetBufferSettingsAsync(Guid tenantId)
    {
        var tenant = await tenantRepository.GetTenantByIdAsync(tenantId);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant not found: {tenantId}");
        }

        tenant.BufferBeforeMinutes = 0;
        tenant.BufferAfterMinutes = 0;

        await tenantRepository.UpdateTenantAsync(tenant);

        logger.LogInformation("Reset buffer settings for tenant {TenantId}", tenantId);
    }
}