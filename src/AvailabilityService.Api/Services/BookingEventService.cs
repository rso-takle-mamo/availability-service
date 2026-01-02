using Microsoft.Extensions.Logging;
using AvailabilityService.Api.Events.Booking;
using AvailabilityService.Api.Services.Interfaces;
using AvailabilityService.Database.Repositories.Interfaces;
using AvailabilityService.Database.Entities;

namespace AvailabilityService.Api.Services;

public class BookingEventService(
    ILogger<BookingEventService> logger,
    IBookingRepository bookingRepository) : IBookingEventService
{
    public async Task HandleBookingCreatedEventAsync(BookingCreatedEvent bookingEvent)
    {
        logger.LogInformation("Handling booking created event for booking ID: {BookingId}", bookingEvent.BookingId);

        try
        {
            var existingBooking = await bookingRepository.GetByIdAsync(bookingEvent.BookingId);
            if (existingBooking != null)
            {
                logger.LogWarning("Booking with ID {BookingId} already exists, skipping creation", bookingEvent.BookingId);
                return;
            }

            var booking = new Booking
            {
                Id = bookingEvent.BookingId,
                TenantId = bookingEvent.TenantId,
                OwnerId = bookingEvent.OwnerId,
                StartDateTime = bookingEvent.StartDateTime,
                EndDateTime = bookingEvent.EndDateTime,
                BookingStatus = bookingEvent.BookingStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await bookingRepository.CreateAsync(booking);
            logger.LogInformation("Successfully created booking {BookingId} in availability database", bookingEvent.BookingId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling booking created event for booking ID: {BookingId}", bookingEvent.BookingId);
            throw;
        }
    }

    public async Task HandleBookingCancelledEventAsync(BookingCancelledEvent bookingEvent)
    {
        logger.LogInformation("Handling booking cancelled event for booking ID: {BookingId}", bookingEvent.BookingId);

        try
        {
            var existingBooking = await bookingRepository.GetByIdAsync(bookingEvent.BookingId);
            if (existingBooking == null)
            {
                logger.LogWarning("Booking with ID {BookingId} not found for cancellation", bookingEvent.BookingId);
                return;
            }

            existingBooking.BookingStatus = BookingStatus.Cancelled;
            existingBooking.UpdatedAt = DateTime.UtcNow;

            await bookingRepository.UpdateAsync(existingBooking);
            logger.LogInformation("Successfully cancelled booking {BookingId} in availability database", bookingEvent.BookingId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling booking cancelled event for booking ID: {BookingId}", bookingEvent.BookingId);
            throw;
        }
    }
}
