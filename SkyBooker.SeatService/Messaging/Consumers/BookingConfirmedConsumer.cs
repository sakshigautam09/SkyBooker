using MassTransit;
using SkyBooker.SeatService.Messaging.Events;
using SkyBooker.SeatService.Services;
using SkyBooker.SeatService.Services.Redis;

namespace SkyBooker.SeatService.Messaging.Consumers;

/// <summary>
/// Consumes BookingConfirmedEvent from Booking-Service (after payment success).
/// SAGA step: moves all seats HELD → CONFIRMED in DB, removes Redis hold keys.
/// </summary>
public class BookingConfirmedConsumer : IConsumer<BookingConfirmedEvent>
{
    private readonly ISeatService _seatService;
    private readonly ISeatHoldRedisService _redisService;
    private readonly ILogger<BookingConfirmedConsumer> _logger;

    public BookingConfirmedConsumer(
        ISeatService seatService,
        ISeatHoldRedisService redisService,
        ILogger<BookingConfirmedConsumer> logger)
    {
        _seatService = seatService;
        _redisService = redisService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookingConfirmedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[RabbitMQ] BookingConfirmedEvent received — PNR={Pnr}, PaymentId={PaymentId}",
            msg.PnrCode, msg.PaymentId);

        foreach (var seatId in msg.SeatIds)
        {
            try
            {
                // 1. Confirm in DB: HELD → CONFIRMED
                await _seatService.ConfirmSeatAsync(seatId);

                // 2. Remove Redis hold key (seat is now permanently confirmed)
                await _redisService.ReleaseSeatHoldAsync(seatId);

                _logger.LogInformation(
                    "[SAGA] Seat {SeatId} confirmed — PNR={Pnr}", seatId, msg.PnrCode);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(
                    "[SAGA] Seat {SeatId} not found during confirm: {Msg}", seatId, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // Already confirmed — idempotent
                _logger.LogInformation(
                    "[SAGA] Seat {SeatId} already confirmed: {Msg}", seatId, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SAGA] Error confirming Seat {SeatId} for PNR={Pnr}", seatId, msg.PnrCode);
                throw;
            }
        }
    }
}
