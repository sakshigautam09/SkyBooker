using SkyBooker.PaymentService.Messaging.Events;

namespace SkyBooker.PaymentService.Messaging.Publishers;

public interface IPaymentEventPublisher
{
    Task PublishPaymentCompletedAsync(PaymentCompletedEvent @event);
    Task PublishPaymentFailedAsync(PaymentFailedEvent @event);
    Task PublishPaymentRefundedAsync(PaymentRefundedEvent @event);
}
