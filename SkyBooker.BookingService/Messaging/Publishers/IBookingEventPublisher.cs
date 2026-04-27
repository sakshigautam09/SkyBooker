using SkyBooker.BookingService.Messaging.Events;

namespace SkyBooker.BookingService.Messaging.Publishers;

public interface IBookingEventPublisher
{
    Task PublishBookingCreatedAsync(BookingCreatedEvent @event);
    Task PublishBookingConfirmedAsync(BookingConfirmedEvent @event);
    Task PublishBookingCancelledAsync(BookingCancelledEvent @event);
}
