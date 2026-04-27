using MassTransit;
using SkyBooker.PaymentService.Messaging.Events;

namespace SkyBooker.PaymentService.Messaging.Publishers;

public class PaymentEventPublisher : IPaymentEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PaymentEventPublisher> _logger;

    public PaymentEventPublisher(IPublishEndpoint publishEndpoint, ILogger<PaymentEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishPaymentCompletedAsync(PaymentCompletedEvent @event)
    {
        await _publishEndpoint.Publish(@event);
        _logger.LogInformation(
            "[RabbitMQ] Published PaymentCompletedEvent — PaymentId={Id}, BookingId={BookingId}, Amount={Amount}",
            @event.PaymentId, @event.BookingId, @event.Amount);
    }

    public async Task PublishPaymentFailedAsync(PaymentFailedEvent @event)
    {
        await _publishEndpoint.Publish(@event);
        _logger.LogWarning(
            "[RabbitMQ] Published PaymentFailedEvent — PaymentId={Id}, BookingId={BookingId}, Reason={Reason}",
            @event.PaymentId, @event.BookingId, @event.Reason);
    }

    public async Task PublishPaymentRefundedAsync(PaymentRefundedEvent @event)
    {
        await _publishEndpoint.Publish(@event);
        _logger.LogInformation(
            "[RabbitMQ] Published PaymentRefundedEvent — PaymentId={Id}, BookingId={BookingId}, RefundAmount={Amount}",
            @event.PaymentId, @event.BookingId, @event.RefundAmount);
    }
}
