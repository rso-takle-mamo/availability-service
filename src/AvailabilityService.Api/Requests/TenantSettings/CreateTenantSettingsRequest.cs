namespace AvailabilityService.Api.Requests.TenantSettings;

public class CreateTenantSettingsRequest
{
    public required string BusinessName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TimeZone { get; set; }
    public int? BufferBeforeMinutes { get; set; }
    public int? BufferAfterMinutes { get; set; }
}