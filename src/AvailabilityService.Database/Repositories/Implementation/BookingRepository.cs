using Microsoft.EntityFrameworkCore;
using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Repositories.Interfaces;

namespace AvailabilityService.Database.Repositories.Implementation;

public class BookingRepository(AvailabilityDbContext context) : IBookingRepository
{
    public async Task<List<Booking>> GetBookingsByTenantAndDateRangeAsync(Guid tenantId, DateTime startDate, DateTime endDate)
    {
        return await context.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId &&
                       (b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.Pending) &&
                       b.StartDateTime <= endDate &&
                       b.EndDateTime >= startDate)
            .ToListAsync();
    }
}