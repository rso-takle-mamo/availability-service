using AvailabilityService.Database.Entities;

namespace AvailabilityService.Database.Repositories.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<List<Booking>> GetBookingsByTenantAndDateRangeAsync(Guid tenantId, DateTime startDate, DateTime endDate);
    Task<Booking> CreateAsync(Booking booking);
    Task<Booking> UpdateAsync(Booking booking);
    Task DeleteAsync(Booking booking);
}