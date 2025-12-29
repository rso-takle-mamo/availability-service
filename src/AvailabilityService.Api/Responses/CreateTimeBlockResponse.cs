using AvailabilityService.Database.Entities;

namespace AvailabilityService.Api.Responses;

public class CreateTimeBlockResponse : TimeBlockResponse
{
    public int TotalCreated { get; set; }
}