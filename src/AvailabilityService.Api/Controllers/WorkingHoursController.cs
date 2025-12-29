using Microsoft.AspNetCore.Mvc;
using AvailabilityService.Api.Requests.WorkingHours;
using AvailabilityService.Api.Responses;
using AvailabilityService.Api.Services.Interfaces;
using AvailabilityService.Api.Exceptions;
using AvailabilityService.Api.Models;
using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Repositories.Interfaces;

namespace AvailabilityService.Api.Controllers;

[ApiController]
[Route("api/availability")]
public class WorkingHoursController(
    ILogger<WorkingHoursController> logger,
    IWorkingHoursRepository workingHoursRepository,
    IUserContextService userContextService)
    : BaseApiController(userContextService)
{
    /// <summary>
    /// Get working hours for a tenant
    /// </summary>
    /// <remarks>
    /// **CUSTOMERS:**
    /// - Must provide tenantId query parameter
    /// - Can view working hours for any tenant
    ///
    /// **PROVIDERS:**
    /// - Cannot provide tenantId parameter
    /// - Can only view their own working hours
    /// </remarks>
    /// <param name="tenantId">Tenant ID (required for customers, forbidden for providers)</param>
    /// <param name="day">Optional day of week to filter by</param>
    /// <returns>List of working hours</returns>
    [HttpGet("working-hours")]
    public async Task<IActionResult> GetWorkingHours([FromQuery] Guid? tenantId = null, [FromQuery] DayOfWeek? day = null)
    {
            Guid targetTenantId;
            if (IsCustomer())
            {
                if (!tenantId.HasValue)
                {
                    throw new ValidationException("tenantId query parameter is required for customers",
                    [
                        new ValidationError
                            { Field = "tenantId", Message = "tenantId query parameter is required for customers" }
                    ]);
                }
                targetTenantId = tenantId.Value;
            }
            else
            {
                if (tenantId.HasValue)
                {
                    throw new ValidationException("Providers should not provide tenantId parameter. They can only access their own working hours.",
                    [
                        new ValidationError
                        {
                            Field = "tenantId",
                            Message =
                                "Providers should not provide tenantId parameter. They can only access their own working hours."
                        }
                    ]);
                }
                targetTenantId = GetTenantId() ?? throw new AuthorizationException("WorkingHours", "read", "Providers must have a tenant ID.");
            }

            IEnumerable<WorkingHours> workingHours;

            if (day.HasValue)
            {
                var dayWorkingHours = await workingHoursRepository.GetWorkingHoursByTenantAndDayAsync(targetTenantId, day.Value);
                workingHours = dayWorkingHours != null ? [dayWorkingHours] : [];
            }
            else
            {
                workingHours = await workingHoursRepository.GetWorkingHoursByTenantAsync(targetTenantId);
            }

            var response = workingHours.Select(wh => new WorkingHoursResponse
            {
                Id = wh.Id,
                TenantId = wh.TenantId,
                Day = wh.Day,
                StartTime = wh.StartTime,
                EndTime = wh.EndTime,
                MaxConcurrentBookings = wh.MaxConcurrentBookings,
                CreatedAt = wh.CreatedAt,
                UpdatedAt = wh.UpdatedAt
            });

            return Ok(response);
    }
    
    /// <summary>
    /// Create working hours for a specific day
    /// </summary>
    /// <remarks>
    /// **PROVIDERS ONLY**
    /// - Can only create working hours for their own tenant
    /// - Cannot create working hours if they already exist for the day (use PUT to update)
    /// </remarks>
    /// <param name="request">Working hours details</param>
    /// <returns>Created working hours</returns>
    [HttpPost("working-hours")]
    public async Task<IActionResult> CreateWorkingHours([FromBody] CreateWorkingHoursRequest request)
    {
        ValidateProviderAccess();

        var tenantId = GetTenantId() ?? throw new AuthorizationException("WorkingHours", "write", "Providers must have a tenant ID.");

        logger.LogInformation("Creating working hours for tenant: {TenantId}, day: {Day}", tenantId, request.Day);

        if (request.StartTime >= request.EndTime)
        {
            throw new ValidationException("Start time must be before end time",
                [new ValidationError { Field = "startTime", Message = "Start time must be before end time" }]);
        }

        var existingWorkingHours = await workingHoursRepository.GetWorkingHoursByTenantAndDayAsync(tenantId, request.Day);
        if (existingWorkingHours != null)
        {
            throw new ValidationException($"Working hours already exist for {request.Day}. Use PUT endpoint to update.",
            [
                new ValidationError
                {
                    Field = "day",
                    Message = $"Working hours already exist for {request.Day}. Use PUT endpoint to update."
                }
            ]);
        }

        var workingHours = new WorkingHours
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Day = request.Day,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            MaxConcurrentBookings = request.MaxConcurrentBookings ?? 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await workingHoursRepository.CreateWorkingHoursAsync(workingHours);

        var response = new WorkingHoursResponse
        {
            Id = workingHours.Id,
            TenantId = workingHours.TenantId,
            Day = workingHours.Day,
            StartTime = workingHours.StartTime,
            EndTime = workingHours.EndTime,
            MaxConcurrentBookings = workingHours.MaxConcurrentBookings,
            CreatedAt = workingHours.CreatedAt,
            UpdatedAt = workingHours.UpdatedAt
        };

        return CreatedAtAction(nameof(GetWorkingHours), new { tenantId = workingHours.TenantId, day = workingHours.Day }, response);
    }
  
    /// <summary>
    /// Create weekly schedule in batch
    /// </summary>
    /// <remarks>
    /// **PROVIDERS ONLY**
    /// - Replaces all existing working hours with new schedule
    /// - Allows setting work-free days
    /// - Each schedule entry can apply to multiple days
    /// </remarks>
    /// <param name="request">Weekly schedule details</param>
    /// <returns>Summary of created schedule</returns>
    [HttpPost("working-hours/batch")]
    public async Task<IActionResult> CreateWeeklySchedule([FromBody] CreateWeeklyScheduleRequest request)
    {
            ValidateProviderAccess();

            var tenantId = GetTenantId() ?? throw new AuthorizationException("WorkingHours", "write", "Providers must have a tenant ID.");

            logger.LogInformation("Creating weekly schedule for tenant: {TenantId}", tenantId);

            var validationErrors = new List<string>();

            foreach (var entry in request.Schedule)
            {
                if (!entry.IsWorkFree && (!entry.StartTime.HasValue || !entry.EndTime.HasValue))
                {
                    validationErrors.Add($"Start time and end time are required for work days in entry for days: {string.Join(", ", entry.Days)}");
                }

                if (entry.StartTime.HasValue && entry.EndTime.HasValue && entry.StartTime >= entry.EndTime)
                {
                    validationErrors.Add($"Start time must be before end time for days: {string.Join(", ", entry.Days)}");
                }
            }

            if (validationErrors.Count != 0)
            {
                throw new ValidationException("Validation failed",
                    validationErrors.Select(e => new ValidationError { Message = e }).ToList());
            }

            await workingHoursRepository.DeleteWorkingHoursByTenantAsync(tenantId);

            var workingHoursToCreate = new List<WorkingHours>();
            var createdDays = new List<DayOfWeek>();

            foreach (var entry in request.Schedule)
            {
                if (!entry.IsWorkFree)
                {
                    foreach (var day in entry.Days)
                    {
                        var workingHours = new WorkingHours
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            Day = day,
                            StartTime = entry.StartTime!.Value,
                            EndTime = entry.EndTime!.Value,
                            MaxConcurrentBookings = entry.MaxConcurrentBookings ?? 1,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        workingHoursToCreate.Add(workingHours);
                        createdDays.Add(day);
                    }
                }
            }

            var createdCount = await workingHoursRepository.CreateMultipleWorkingHoursAsync(workingHoursToCreate);

            return Ok(new
            {
                Message = "Weekly schedule created successfully",
                CreatedCount = createdCount,
                CreatedDays = createdDays.Select(d => d.ToString()).ToList(),
                FreeDays = Enum.GetValues<DayOfWeek>()
                    .Except(createdDays)
                    .Select(d => d.ToString())
                    .ToList()
            });
    }
        
    /// <summary>
    /// Delete working hours
    /// </summary>
    /// <remarks>
    /// **PROVIDERS ONLY**
    /// - Can only delete working hours belonging to their tenant
    /// - Working hours must exist before deletion
    /// </remarks>
    /// <param name="id">Working hours ID</param>
    /// <returns>No content on successful deletion</returns>
    [HttpDelete("working-hours/{id}")]
    public async Task<IActionResult> DeleteWorkingHours(Guid id)
    {
        ValidateProviderAccess();

        var tenantId = GetTenantId() ?? throw new AuthorizationException("WorkingHours", "write", "Providers must have a tenant ID.");

        logger.LogInformation("Deleting working hours: {Id} for tenant: {TenantId}", id, tenantId);

        var existingWorkingHours = await workingHoursRepository.GetWorkingHoursByIdAsync(id);
        if (existingWorkingHours == null)
        {
            throw new NotFoundException("WorkingHours", id);
        }

        if (existingWorkingHours.TenantId != tenantId)
        {
            throw new AuthorizationException("workingHours", "delete", "You are not authorized to delete these working hours");
        }

        var success = await workingHoursRepository.DeleteWorkingHoursAsync(id);
        return !success ? throw new DatabaseOperationException("Failed to delete working hours") : NoContent();
    }
}