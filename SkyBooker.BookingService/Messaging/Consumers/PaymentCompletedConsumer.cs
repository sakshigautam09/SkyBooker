using MassTransit;
using SkyBooker.BookingService.DTOs;
using SkyBooker.BookingService.Messaging.Events;
using SkyBooker.BookingService.Services;

namespace SkyBooker.BookingService.Messaging.Consumers;

/// <summary>
/// Consumes PaymentCompletedEvent from Payment-Service.
/// SAGA step: moves Booking status Pending → Confirmed,
///            then publishes BookingConfirmedEvent so Seat-Service and
///            Notification-Service can react.
/// </summary>
public class PaymentCompletedConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<PaymentCompletedConsumer> _logger;

    public PaymentCompletedConsumer(IBookingService bookingService, ILogger<PaymentCompletedConsumer> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[RabbitMQ] Received PaymentCompletedEvent — BookingId={Id}, PaymentId={PaymentId}",
            msg.BookingId, msg.PaymentId);

        try
        {
            // Move booking status to Confirmed and stamp PaymentId
            await _bookingService.UpdateStatusAsync(msg.BookingId, new UpdateBookingStatusDto
            {
                Status = "Confirmed",
                PaymentId = msg.PaymentId
            });

            _logger.LogInformation(
                "[SAGA] Booking {Id} confirmed after payment {PaymentId}",
                msg.BookingId, msg.PaymentId);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(
                "[SAGA] PaymentCompleted received for unknown BookingId={Id}: {Msg}",
                msg.BookingId, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[SAGA] Failed to confirm booking {Id} after payment", msg.BookingId);
            throw; // MassTransit will retry via its retry policy
        }
    }
}
