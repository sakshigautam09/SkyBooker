using MassTransit;
using SkyBooker.BookingService.Messaging.Events;

namespace SkyBooker.BookingService.Messaging.Publishers;

public class BookingEventPublisher : IBookingEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<BookingEventPublisher> _logger;

    public BookingEventPublisher(IPublishEndpoint publishEndpoint, ILogger<BookingEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishBookingCreatedAsync(BookingCreatedEvent @event)
    {
        await _publishEndpoint.Publish(@event);
        _logger.LogInformation(
            "[RabbitMQ] Published BookingCreatedEvent — PNR={Pnr}, BookingId={Id}",
            @event.PnrCode, @event.BookingId);
    }

    public async Task PublishBookingConfirmedAsync(BookingConfirmedEvent @event)
    {
        await _publishEndpoint.Publish(@event);
        _logger.LogInformation(
            "[RabbitMQ] Published BookingConfirmedEvent — PNR={Pnr}, PaymentId={PaymentId}",
            @event.PnrCode, @event.PaymentId);
    }

    public async Task PublishBookingCancelledAsync(BookingCancelledEvent @event)
    {
        await _publishEndpoint.Publish(@event);
        _logger.LogInformation(
            "[RabbitMQ] Published BookingCancelledEvent (SAGA compensation) — PNR={Pnr}",
            @event.PnrCode);
    }
}
