using System.ComponentModel.DataAnnotations;

namespace AvailabilityService.Api.Requests.TimeBlock;

/// <summary>
/// Request to delete multiple time blocks within a date range
/// </summary>
public class BulkDeleteTimeBlocksRequest
{
    /// <summary>
    /// Start date of the range for bulk deletion
    /// </summary>
    /// <example>2025-12-24T00:00:00Z</example>
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the range for bulk deletion
    /// </summary>
    /// <example>2025-12-31T23:59:59Z</example>
    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }
}