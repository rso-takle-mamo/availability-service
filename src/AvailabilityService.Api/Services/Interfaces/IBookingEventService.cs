using AvailabilityService.Api.Events.Booking;

namespace AvailabilityService.Api.Services.Interfaces;

public interface IBookingEventService
{
    Task HandleBookingCreatedEventAsync(BookingCreatedEvent bookingEvent);
    Task HandleBookingCancelledEventAsync(BookingCancelledEvent bookingEvent);
}
