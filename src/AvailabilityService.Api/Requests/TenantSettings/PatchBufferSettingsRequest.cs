using System.ComponentModel.DataAnnotations;

namespace AvailabilityService.Api.Requests.TenantSettings;

/// <summary>
    /// Request to update buffer settings
    /// </summary>
public class PatchBufferSettingsRequest
{
    /// <summary>
    /// Buffer time in minutes before appointments (null = no change)
    /// </summary>
    /// <example>15</example>
    [Range(0, 480, ErrorMessage = "Buffer before minutes must be between 0 and 480")]
    public int? BufferBeforeMinutes { get; set; }

    /// <summary>
    /// Buffer time in minutes after appointments (null = no change)
    /// </summary>
    /// <example>15</example>
    [Range(0, 480, ErrorMessage = "Buffer after minutes must be between 0 and 480")]
    public int? BufferAfterMinutes { get; set; }
}