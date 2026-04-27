using MassTransit;
using SkyBooker.BookingService.Messaging.Events;
using SkyBooker.BookingService.Services;

namespace SkyBooker.BookingService.Messaging.Consumers;

/// <summary>
/// Consumes PaymentFailedEvent from Payment-Service.
/// SAGA compensation: cancels the booking, which triggers BookingCancelledEvent
///                    so Seat-Service releases the held seats and
///                    Flight-Service increments AvailableSeats back.
/// </summary>
public class PaymentFailedConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<PaymentFailedConsumer> _logger;

    public PaymentFailedConsumer(IBookingService bookingService, ILogger<PaymentFailedConsumer> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var msg = context.Message;
        _logger.LogWarning(
            "[RabbitMQ] Received PaymentFailedEvent — BookingId={Id}, Reason={Reason}",
            msg.BookingId, msg.Reason);

        try
        {
            // SAGA compensation: cancel the booking
            // CancelBookingAsync internally publishes BookingCancelledEvent
            await _bookingService.CancelBookingAsync(msg.BookingId);

            _logger.LogInformation(
                "[SAGA] Compensation complete — Booking {Id} cancelled due to payment failure",
                msg.BookingId);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(
                "[SAGA] PaymentFailed received for unknown BookingId={Id}: {Msg}",
                msg.BookingId, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // Already cancelled — idempotent, safe to ignore
            _logger.LogInformation(
                "[SAGA] Booking {Id} already cancelled: {Msg}",
                msg.BookingId, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[SAGA] Compensation failed for booking {Id}", msg.BookingId);
            throw;
        }
    }
}
