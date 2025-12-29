using Microsoft.AspNetCore.Mvc;
using AvailabilityService.Api.Services.Interfaces;
using AvailabilityService.Api.Exceptions;
using AvailabilityService.Api.Models;

namespace AvailabilityService.Api.Controllers;

[Route("api/availability")]
public class AvailabilityController(
    ILogger<AvailabilityController> logger,
    IAvailabilityService availabilityService,
    IUserContextService userContextService)
    : BaseApiController(userContextService)
{
    /// <summary>
    /// Get available time slots for booking
    /// </summary>
    /// <remarks>
    /// **CUSTOMERS:**
    /// - Must provide tenantId query parameter
    /// - Can check availability for any tenant
    ///
    /// **PROVIDERS:**
    /// - Cannot provide tenantId parameter
    /// - Can only check availability for their own tenant
    /// - Date range cannot exceed 1 month
    /// </remarks>
    /// <param name="tenantId">Tenant ID (required for customers, forbidden for providers)</param>
    /// <param name="startDate">Start date for availability check (required, format: 2026-01-01Z)</param>
    /// <param name="endDate">End date for availability check (required, format: 2026-01-01Z)</param>
    /// <returns>List of available time ranges</returns>
    [HttpGet("slots")]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var missingFields = new List<string>();
                if (!startDate.HasValue) missingFields.Add("startDate");
                if (!endDate.HasValue) missingFields.Add("endDate");

                throw new ValidationException($"Missing required parameters: {string.Join(", ", missingFields)}",
                    missingFields.Select(f => new ValidationError { Field = f, Message = $"{f} is required" }).ToList());
            }

            if (endDate.Value - startDate.Value > TimeSpan.FromDays(31))
            {
                throw new ValidationException("Date range cannot exceed 1 month",
                    [new ValidationError { Field = "endDate", Message = "Date range cannot exceed 1 month" }]);
            }

            Guid targetTenantId;

            if (IsCustomer())
            {
                // Customer must provide tenantId in query
                if (!tenantId.HasValue)
                {
                    throw new ValidationException("tenantId query parameter is required for customers",
                        [new ValidationError() { Field = "tenantId", Message = "tenantId query parameter is required for customers" }]);
                }
                targetTenantId = tenantId.Value;
                ValidateTenantAccess(targetTenantId);
            }
            else // Provider
            {
                // Provider should NOT provide tenantId in query, use their own
                if (tenantId.HasValue)
                {
                    throw new ValidationException("Providers should not provide tenantId parameter. They can only check their own availability.",
                    [
                        new ValidationError
                        {
                            Field = "tenantId",
                            Message =
                                "Providers should not provide tenantId parameter. They can only check their own availability."
                        }
                    ]);
                }
                targetTenantId = GetTenantId() ?? throw new AuthorizationException("Availability", "read", "Providers must have a tenant ID.");
            }

            logger.LogInformation("Getting available time ranges for tenant: {TenantId}, from: {StartDate} to: {EndDate}",
                targetTenantId, startDate, endDate);

            var response = await availabilityService.GetAvailableRangesAsync(
                targetTenantId,
                startDate!.Value,
                endDate!.Value);

            return Ok(response);
    }
}