using Microsoft.EntityFrameworkCore;
using AvailabilityService.Database.Entities;
using AvailabilityService.Database.Repositories.Interfaces;

namespace AvailabilityService.Database.Repositories.Implementation;

public class BookingRepository(AvailabilityDbContext context) : IBookingRepository
{
    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

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

    public async Task<Booking> CreateAsync(Booking booking)
    {
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        return booking;
    }

    public async Task<Booking> UpdateAsync(Booking booking)
    {
        context.Bookings.Update(booking);
        await context.SaveChangesAsync();
        return booking;
    }

    public async Task DeleteAsync(Booking booking)
    {
        context.Bookings.Remove(booking);
        await context.SaveChangesAsync();
    }
}