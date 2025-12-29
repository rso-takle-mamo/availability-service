using Microsoft.AspNetCore.Mvc;
using AvailabilityService.Api.Requests.TimeBlock;
using AvailabilityService.Api.Responses;
using AvailabilityService.Api.Services.Interfaces;
using AvailabilityService.Api.Exceptions;
using AvailabilityService.Api.Models;
using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Repositories.Interfaces;
using AvailabilityService.Database.Models;
using ValidationException = AvailabilityService.Api.Exceptions.ValidationException;

namespace AvailabilityService.Api.Controllers;

[Route("api/availability")]
public class TimeBlockController(
    ILogger<TimeBlockController> logger,
    ITimeBlockRepository timeBlockRepository,
    IRecurrenceService recurrenceService,
    IUserContextService userContextService)
    : BaseApiController(userContextService)
{
    /// <summary>
    /// Get time blocks (unavailable periods) for the provider's tenant
    /// </summary>
    /// <remarks>
    /// **PROVIDERS ONLY**
    /// - Can only access their own time blocks
    /// - Supports pagination
    /// - Optional date range filtering
    /// </remarks>
    /// <param name="pagination">Pagination parameters (offset and limit)</param>
    /// <param name="startDate">Optional start date to filter time blocks (format: 2026-01-01Z)</param>
    /// <param name="endDate">Optional end date to filter time blocks (format: 2026-01-01Z)</param>
    /// <returns>Paginated list of time blocks</returns>
    [HttpGet("time-blocks")]
    public async Task<IActionResult> GetTimeBlocks([FromQuery] PaginationParameters pagination, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        ValidateProviderAccess();

        var targetTenantId = GetTenantId() ?? throw new AuthorizationException("TimeBlock", "read", "Providers must have a tenant ID.");

        logger.LogInformation("Getting time blocks for tenant: {TenantId}, startDate: {StartDate}, endDate: {EndDate}, offset: {Offset}, limit: {Limit}",
            targetTenantId, startDate, endDate, pagination.Offset, pagination.Limit);

        IEnumerable<TimeBlock> timeBlocks;
        int totalCount;

        if (startDate.HasValue && endDate.HasValue)
        {
            var (blocks, count) = await timeBlockRepository.GetTimeBlocksByDateRangeAsync(startDate.Value, endDate.Value, targetTenantId);
            timeBlocks = blocks;
            totalCount = count;
        }
        else
        {
            var (blocks, count) = await timeBlockRepository.GetTimeBlocksAsync(pagination, targetTenantId);
            timeBlocks = blocks;
            totalCount = count;
        }

        var response = new PaginatedResponse<TimeBlockResponse>
        {
            Offset = pagination.Offset,
            Limit = pagination.Limit,
            TotalCount = totalCount,
            Data = timeBlocks.Select(tb => new TimeBlockResponse
            {
                Id = tb.Id,
                TenantId = tb.TenantId,
                StartDateTime = tb.StartDateTime,
                EndDateTime = tb.EndDateTime,
                Type = tb.Type,
                Reason = tb.Reason,
                IsRecurring = tb.IsRecurring,
                CreatedAt = tb.CreatedAt,
                UpdatedAt = tb.UpdatedAt
            }).ToList()
        };

        return Ok(response);
    }
      /// <summary>
    /// Get a specific time block by ID
    /// </summary>
    /// <remarks>
    /// **PROVIDERS ONLY**
    /// - Can only access time blocks belonging to their tenant
    /// </remarks>
    /// <param name="id">Time block ID</param>
    /// <returns>Time block details</returns>
    [HttpGet("time-blocks/{id}")]
    public async Task<IActionResult> GetTimeBlockById(Guid id)
    {
        logger.LogInformation("Getting time block: {Id}", id);

        var timeBlock = await timeBlockRepository.GetTimeBlockByIdAsync(id);
        if (timeBlock == null)
        {
            throw new NotFoundException("TimeBlock", id);
        }

        ValidateProviderAccess();

        var providerTenantId = GetTenantId() ?? throw new AuthorizationException("TimeBlock", "write", "Providers must have a tenant ID.");
        if (timeBlock.TenantId != providerTenantId)
        {
            throw new AuthorizationException("timeBlock", "access", "You are not authorized to access this time block");
        }

        var response = new TimeBlockResponse
        {
            Id = timeBlock.Id,
            TenantId = timeBlock.TenantId,
            StartDateTime = timeBlock.StartDateTime,
            EndDateTime = timeBlock.EndDateTime,
            Type = timeBlock.Type,
            Reason = timeBlock.Reason,
            IsRecurring = timeBlock.IsRecurring,
            CreatedAt = timeBlock.CreatedAt,
            UpdatedAt = timeBlock.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new time block (unavailable period)
    /// </summary>
    /// <remarks>
    /// **PROVIDERS ONLY**
    /// - Can only create time blocks for their own tenant
    /// - Supports recurring patterns with the following options:
    ///   - **Daily**: Repeats every day or every N days
    ///     - Use `interval` to specify days between occurrences
    ///     - Example: Every 2 days = interval: 2
    ///   - **Weekly**: Repeats every week or every N weeks on specific days
    ///     - Use `interval` for weeks between occurrences
    ///     - Use `daysOfWeek` array (0=Sunday, 6=Saturday)
    ///     - Example: Every week on Mon, Wed, Fri = interval: 1, daysOfWeek: [1, 3, 5]
    ///   - **Monthly**: Repeats every month or every N months on specific days
    ///     - Use `interval` for months between occurrences
    ///     - Use `daysOfMonth` array (1-31 for specific days, negative for from end)
    ///     - Example: Every month on 15th and last day = interval: 1, daysOfMonth: [15, -1]
    /// - Cannot create time blocks in the past
    /// - Types: Vacation, Break, Custom
    /// - **End Condition** (required for recurrence):
    ///   - `endDate`: Last date for recurrence (exclusive)
    ///   - `maxOccurrences`: Maximum number of occurrences to create
    /// </remarks>
    /// <param name="request">Time block details including optional recurrence pattern</param>
    /// <returns>Created time block with count of total blocks created (for recurring)</returns>
    [HttpPost("time-blocks")]
    public async Task<IActionResult> CreateTimeBlock([FromBody] CreateTimeBlockRequest request)
    {
        ValidateProviderAccess();

        var tenantId = GetTenantId() ?? throw new AuthorizationException("TimeBlock", "write", "Providers must have a tenant ID.");

        logger.LogInformation("Creating time block for tenant: {TenantId}, type: {Type}", tenantId, request.Type);

        if (request.StartDateTime >= request.EndDateTime)
        {
            throw new ValidationException("Start time must be before end time",
                [new ValidationError { Field = "startDateTime", Message = "Start time must be before end time" }]);
        }

        var today = DateTime.UtcNow.Date;
        if (request.StartDateTime.Date < today)
        {
            throw new ValidationException("Start date cannot be in the past",
                [new ValidationError { Field = "startDateTime", Message = "Start date cannot be in the past" }]);
        }
        if (request.EndDateTime.Date < today)
        {
            throw new ValidationException("End date cannot be in the past",
                [new ValidationError { Field = "endDateTime", Message = "End date cannot be in the past" }]);
        }

        RecurrencePattern? recurrencePatternEntity = null;
        if (request.RecurrencePattern != null)
        {
            var validationErrors = ValidateRecurrencePattern(request.RecurrencePattern, request.StartDateTime);
            if (validationErrors.Any())
            {
                var errors = validationErrors.Select(e => new ValidationError { Field = "recurrencePattern", Message = e }).ToList();
                throw new ValidationException("Invalid recurrence pattern", errors);
            }
            recurrencePatternEntity = ConvertToRecurrencePattern(request.RecurrencePattern, request.StartDateTime);
        }

        var recurrenceId = recurrencePatternEntity != null ? Guid.NewGuid() : (Guid?)null;

        var timeBlock = new TimeBlock
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Type = ParseTimeBlockType(request.Type),
            Reason = request.Reason,
            RecurrenceId = recurrenceId
        };

        await timeBlockRepository.CreateTimeBlockAsync(timeBlock);

        var totalCreated = 1;

        if (request.RecurrencePattern != null && recurrenceId.HasValue)
        {
            var recurringBlocks = await recurrenceService.GenerateRecurringTimeBlocksAsync(
                recurrencePatternEntity!,
                request.StartDateTime,
                request.EndDateTime,
                recurrenceId.Value,
                tenantId,
                ParseTimeBlockType(request.Type),
                request.Reason
            );
            totalCreated += recurringBlocks.Count;
        }

        var response = new CreateTimeBlockResponse
        {
            Id = timeBlock.Id,
            TenantId = timeBlock.TenantId,
            StartDateTime = timeBlock.StartDateTime,
            EndDateTime = timeBlock.EndDateTime,
            Type = timeBlock.Type,
            Reason = timeBlock.Reason,
            IsRecurring = timeBlock.IsRecurring,
            CreatedAt = timeBlock.CreatedAt,
            UpdatedAt = timeBlock.UpdatedAt,
            TotalCreated = totalCreated
        };

        return CreatedAtAction(nameof(GetTimeBlockById), new { id = timeBlock.Id }, response);
    }
    
    /// <summary>
    /// Update a time block (partial update)
    /// </summary>
    /// <remarks>
    /// **PROVIDERS ONLY**
    /// - Can only update time blocks belonging to their tenant
    /// - For recurring blocks, use editPattern=true to update all occurrences
    /// - editPattern=false updates only the specific instance
    /// </remarks>
    /// <param name="id">Time block ID</param>
    /// <param name="request">Fields to update</param>
    /// <param name="editPattern">Whether to edit the entire recurring pattern (for recurring time blocks)</param>
    /// <returns>Updated time block</returns>
    [HttpPatch("time-blocks/{id}")]
    public async Task<IActionResult> PatchTimeBlock(Guid id, [FromBody] PatchTimeBlockRequest request, [FromQuery] bool editPattern = false)
    {
        ValidateProviderAccess();

        var tenantId = GetTenantId() ?? throw new AuthorizationException("TimeBlock", "write", "Providers must have a tenant ID.");

        logger.LogInformation("Updating time block: {Id} for tenant: {TenantId}", id, tenantId);

        var existingTimeBlock = await timeBlockRepository.GetTimeBlockByIdAsync(id);
        if (existingTimeBlock == null)
        {
            throw new NotFoundException("TimeBlock", id);
        }

        if (existingTimeBlock.TenantId != tenantId)
        {
            throw new AuthorizationException("timeBlock", "update", "You are not authorized to update this time block");
        }

        if (request.StartTime.HasValue && request.EndTime.HasValue)
        {
            if (request.StartTime.Value >= request.EndTime.Value)
            {
                throw new ValidationException("Start time must be before end time",
                    [new ValidationError { Field = "startTime", Message = "Start time must be before end time" }]);
            }
        }
        else if (request.EndTime.HasValue)
        {
            var existingStartTime = existingTimeBlock.StartDateTime.TimeOfDay;
            if (request.EndTime.Value.CompareTo(existingStartTime) <= 0)
            {
                throw new ValidationException("End time must be after current start time",
                    [new ValidationError { Field = "endTime", Message = "End time must be after current start time" }]);
            }
        }
        else if (request.StartTime.HasValue)
        {
            var existingEndTime = existingTimeBlock.EndDateTime.TimeOfDay;
            if (request.StartTime.Value.CompareTo(existingEndTime) >= 0)
            {
                throw new ValidationException("Start time must be before current end time",
                [
                    new ValidationError { Field = "startTime", Message = "Start time must be before current end time" }
                ]);
            }
        }
        if (existingTimeBlock == null)
        {
            throw new NotFoundException("TimeBlock", id);
        }

        if (existingTimeBlock.TenantId != tenantId)
        {
            throw new AuthorizationException("timeBlock", "update", "You are not authorized to update this time block");
        }

        var updateRequest = new Database.UpdateModels.UpdateTimeBlock
        {
            Type = request.Type != null ? ParseTimeBlockType(request.Type) : null,
            Reason = request.Reason
        };

        if (request.StartTime.HasValue)
            updateRequest.StartDateTime = existingTimeBlock.StartDateTime.Date + request.StartTime.Value.ToTimeSpan();

        if (request.EndTime.HasValue)
            updateRequest.EndDateTime = existingTimeBlock.EndDateTime.Date + request.EndTime.Value.ToTimeSpan();

        if (editPattern && existingTimeBlock.RecurrenceId.HasValue)
        {
            var allBlocks = await timeBlockRepository.GetTimeBlocksByRecurrenceIdAsync(existingTimeBlock.RecurrenceId.Value, tenantId);

            foreach (var block in allBlocks)
            {
                var blockUpdate = new Database.UpdateModels.UpdateTimeBlock
                {
                    Type = request.Type != null ? ParseTimeBlockType(request.Type) : null,
                    Reason = request.Reason
                };

                if (request.StartTime.HasValue)
                    blockUpdate.StartDateTime = block.StartDateTime.Date + request.StartTime.Value.ToTimeSpan();

                if (request.EndTime.HasValue)
                    blockUpdate.EndDateTime = block.EndDateTime.Date + request.EndTime.Value.ToTimeSpan();

                await timeBlockRepository.UpdateTimeBlockAsync(block.Id, blockUpdate);
            }
        }
        else
        {
            var success = await timeBlockRepository.UpdateTimeBlockAsync(id, updateRequest);
            if (!success)
            {
                throw new DatabaseOperationException("Failed to update time block");
            }
        }

        var updatedTimeBlock = await timeBlockRepository.GetTimeBlockByIdAsync(id);
        if (updatedTimeBlock == null)
        {
            throw new DatabaseOperationException("Time block not found after update");
        }

        var response = new TimeBlockResponse
        {
            Id = updatedTimeBlock.Id,
            TenantId = updatedTimeBlock.TenantId,
            StartDateTime = updatedTimeBlock.StartDateTime,
            EndDateTime = updatedTimeBlock.EndDateTime,
            Type = updatedTimeBlock.Type,
            Reason = updatedTimeBlock.Reason,
            IsRecurring = updatedTimeBlock.IsRecurring,
            CreatedAt = updatedTimeBlock.CreatedAt,
            UpdatedAt = updatedTimeBlock.UpdatedAt
        };

        return Ok(response);
    }


    /// <summary>
    /// Delete a time block
    /// </summary>
    /// <remarks>
    /// **PROVIDERS ONLY**
    /// - Can only delete time blocks belonging to their tenant
    /// - For recurring blocks, use deletePattern=true to delete all occurrences
    /// - deletePattern=false deletes only the specific instance
    /// </remarks>
    /// <param name="id">Time block ID</param>
    /// <param name="deletePattern">Whether to delete the entire recurring pattern (for recurring time blocks)</param>
    /// <returns>No content on successful deletion</returns>
    [HttpDelete("time-blocks/{id}")]
    public async Task<IActionResult> DeleteTimeBlock(Guid id, [FromQuery] bool deletePattern = false)
    {
        ValidateProviderAccess();

        var tenantId = GetTenantId() ?? throw new AuthorizationException("TimeBlock", "write", "Providers must have a tenant ID.");

        logger.LogInformation("Deleting time block: {Id} for tenant: {TenantId}, deletePattern: {DeletePattern}", id, tenantId, deletePattern);

        var existingTimeBlock = await timeBlockRepository.GetTimeBlockByIdAsync(id);
        if (existingTimeBlock == null)
        {
            throw new NotFoundException("TimeBlock", id);
        }

        if (existingTimeBlock.TenantId != tenantId)
        {
            throw new AuthorizationException("timeBlock", "delete", "You are not authorized to delete this time block");
        }

        if (deletePattern && existingTimeBlock.RecurrenceId.HasValue)
        {
            await recurrenceService.DeleteRecurringTimeBlocksAsync(existingTimeBlock.RecurrenceId.Value, tenantId);
            logger.LogInformation("Deleted all time blocks in pattern {RecurrenceId}", existingTimeBlock.RecurrenceId.Value);
        }
        else
        {
            var success = await timeBlockRepository.DeleteTimeBlockAsync(id, tenantId);
            if (!success)
            {
                throw new DatabaseOperationException("Failed to delete time block");
            }
        }

        return NoContent();
    }


    /// <summary>
    /// Delete multiple time blocks within a date range
    /// </summary>
    /// <remarks>
    /// **PROVIDERS ONLY**
    /// - Can only delete time blocks belonging to their tenant
    /// - Deletes all time blocks that fall within the specified date range
    /// - Useful for clearing vacation periods or bulk operations
    /// </remarks>
    /// <param name="request">Date range for bulk deletion</param>
    /// <returns>Summary of deleted time blocks</returns>
    [HttpDelete("time-blocks/range")]
    public async Task<IActionResult> DeleteTimeBlocksByDateRange([FromBody] BulkDeleteTimeBlocksRequest request)
    {
        ValidateProviderAccess();

        var tenantId = GetTenantId() ?? throw new AuthorizationException("TimeBlock", "write", "Providers must have a tenant ID.");

        logger.LogInformation("Deleting time blocks for tenant: {TenantId}, startDate: {StartDate}, endDate: {EndDate}",
            tenantId, request.StartDate, request.EndDate);

        if (request.StartDate >= request.EndDate)
        {
            throw new ValidationException("Start date must be before end date",
                [new ValidationError { Field = "startDate", Message = "Start date must be before end date" }]);
        }

        var deletedCount = await timeBlockRepository.DeleteTimeBlocksByDateRangeAsync(request.StartDate, request.EndDate, tenantId);

        logger.LogInformation("Successfully deleted {Count} time blocks for tenant: {TenantId}", deletedCount, tenantId);

        return Ok(new
        {
            Message = "Time blocks deleted successfully",
            DeletedCount = deletedCount
        });
    }

    private static List<string> ValidateRecurrencePattern(RecurrencePatternRequest pattern, DateTime originalStartDateTime)
    {
        var errors = new List<string>();

        if (pattern.Interval.HasValue && pattern.Interval.Value < 1)
        {
            errors.Add("Interval must be a positive number");
        }

        if (pattern.EndDate.HasValue && pattern.MaxOccurrences.HasValue)
        {
            errors.Add("Cannot specify both EndDate and MaxOccurrences. Please provide only one.");
        }
        else if (!pattern.EndDate.HasValue && !pattern.MaxOccurrences.HasValue)
        {
            errors.Add("Must specify either EndDate or MaxOccurrences to define when the recurrence ends.");
        }

        if (pattern.EndDate.HasValue && pattern.EndDate.Value < DateTime.UtcNow.Date)
        {
            errors.Add("End date cannot be in the past");
        }

        if (pattern.MaxOccurrences.HasValue && pattern.MaxOccurrences.Value <= 0)
        {
            errors.Add("MaxOccurrences must be greater than 0");
        }

        switch (pattern.Frequency.ToLowerInvariant())
        {
            case "daily":
                if (pattern.DaysOfWeek != null && pattern.DaysOfWeek.Length > 0)
                {
                    errors.Add("Daily recurrence cannot include DaysOfWeek. Use Weekly frequency instead.");
                }
                if (pattern.DaysOfMonth != null && pattern.DaysOfMonth.Length > 0)
                {
                    errors.Add("Daily recurrence cannot include DaysOfMonth. Use Monthly frequency instead.");
                }
                break;

            case "weekly":
                if (pattern.DaysOfMonth != null && pattern.DaysOfMonth.Length > 0)
                {
                    errors.Add("Weekly recurrence cannot include DaysOfMonth.");
                }

                if (pattern.DaysOfWeek != null)
                {
                    if (pattern.DaysOfWeek.Any(d => d < 0 || d > 6))
                    {
                        errors.Add("DaysOfWeek must contain values between 0 (Sunday) and 6 (Saturday)");
                    }
                    if (pattern.DaysOfWeek.Length != pattern.DaysOfWeek.Distinct().Count())
                    {
                        errors.Add("DaysOfWeek cannot contain duplicate values");
                    }

                    var originalDayOfWeek = (int)originalStartDateTime.DayOfWeek;
                    if (!pattern.DaysOfWeek.Contains(originalDayOfWeek))
                    {
                        errors.Add($"The original time block is on {originalStartDateTime.DayOfWeek}, but this day is not included in the DaysOfWeek array.");
                    }
                }
                break;

            case "monthly":
                if (pattern.DaysOfWeek != null && pattern.DaysOfWeek.Length > 0)
                {
                    errors.Add("Monthly recurrence cannot include DaysOfWeek.");
                }
                
                if (!(pattern.DaysOfMonth == null || pattern.DaysOfMonth.Length == 0))
                {
                    foreach (var day in pattern.DaysOfMonth)
                    {
                        if (day == 0 || day < -31 || day > 31)
                        {
                            errors.Add("DaysOfMonth must be between -31 and -1, or 1 and 31");
                        }
                        else if (day < -1 && day > -31)
                        {
                            errors.Add($"DaysOfMonth special value {day} is invalid. Use -1 for last day, -2 for second to last, etc.");
                        }
                    }

                    var originalDay = originalStartDateTime.Day;
                    var lastDayOfMonth = DateTime.DaysInMonth(originalStartDateTime.Year, originalStartDateTime.Month);

                    var dayMatches = pattern.DaysOfMonth.Contains(originalDay) ||
                                     (pattern.DaysOfMonth.Contains(-1) && originalDay == lastDayOfMonth) ||
                                     (pattern.DaysOfMonth.Contains(-2) && originalDay == lastDayOfMonth - 1) ||
                                     (pattern.DaysOfMonth.Contains(-3) && originalDay == lastDayOfMonth - 2);

                    if (!dayMatches)
                    {
                        errors.Add($"The original time block is on day {originalDay}, but this day is not included in the DaysOfMonth array.");
                    }
                }
                break;

            default:
                errors.Add("Frequency must be one of: Daily, Weekly, Monthly");
                break;
        }

        return errors;
    }

    private static RecurrencePattern ConvertToRecurrencePattern(RecurrencePatternRequest request, DateTime originalStartDateTime)
    {
        var frequency = request.Frequency.ToLowerInvariant() switch
        {
            "daily" => RecurrenceFrequency.Daily,
            "weekly" => RecurrenceFrequency.Weekly,
            "monthly" => RecurrenceFrequency.Monthly,
            _ => throw new ValidationException("Invalid frequency",
                [new ValidationError { Field = "frequency", Message = $"Invalid frequency: {request.Frequency}" }])
        };

        var pattern = new RecurrencePattern
        {
            Frequency = frequency,
            Interval = request.Interval ?? 1,
            EndDate = request.EndDate,
            MaxOccurrences = request.MaxOccurrences
        };

        // Weekly: Always set DaysOfWeek - if not provided, use the original day
        if (frequency == RecurrenceFrequency.Weekly)
        {
            if (request.DaysOfWeek != null && request.DaysOfWeek.Length > 0)
            {
                pattern.DaysOfWeek = request.DaysOfWeek.Select(d => (DayOfWeek)d).ToArray();
            }
            else
            {
                pattern.DaysOfWeek = [originalStartDateTime.DayOfWeek];
            }
        }

        if (request.DaysOfMonth != null && request.DaysOfMonth.Length > 0)
        {
            pattern.DaysOfMonth = request.DaysOfMonth;
        }
        else if (frequency == RecurrenceFrequency.Monthly)
        {
            pattern.DaysOfMonth = [originalStartDateTime.Day];
        }

        return pattern;
    }

    private static TimeBlockType ParseTimeBlockType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "vacation" => TimeBlockType.Vacation,
            "break" => TimeBlockType.Break,
            "custom" => TimeBlockType.Custom,
            _ => throw new ValidationException("Invalid time block type",
            [
                new ValidationError
                {
                    Field = "type",
                    Message = $"Invalid time block type: {type}. Valid types are: Vacation, Break, Custom"
                }
            ])
        };
    }
}