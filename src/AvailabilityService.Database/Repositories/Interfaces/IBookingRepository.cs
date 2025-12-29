using AvailabilityService.Database.Entities;

namespace AvailabilityService.Database.Repositories.Interfaces;

public interface IBookingRepository
{
    Task<List<Booking>> GetBookingsByTenantAndDateRangeAsync(Guid tenantId, DateTime startDate, DateTime endDate);
}