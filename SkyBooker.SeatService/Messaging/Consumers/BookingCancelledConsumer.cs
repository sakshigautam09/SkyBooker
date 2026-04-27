using MassTransit;
using SkyBooker.SeatService.Messaging.Events;
using SkyBooker.SeatService.Services;
using SkyBooker.SeatService.Services.Redis;

namespace SkyBooker.SeatService.Messaging.Consumers;

/// <summary>
/// Consumes BookingCancelledEvent from Booking-Service.
/// SAGA compensation: releases all seats back to AVAILABLE in DB + removes Redis hold keys.
/// Triggered by: user cancellation OR payment failure.
/// </summary>
public class BookingCancelledConsumer : IConsumer<BookingCancelledEvent>
{
    private readonly ISeatService _seatService;
    private readonly ISeatHoldRedisService _redisService;
    private readonly ILogger<BookingCancelledConsumer> _logger;

    public BookingCancelledConsumer(
        ISeatService seatService,
        ISeatHoldRedisService redisService,
        ILogger<BookingCancelledConsumer> logger)
    {
        _seatService = seatService;
        _redisService = redisService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookingCancelledEvent> context)
    {
        var msg = context.Message;
        _logger.LogWarning(
            "[RabbitMQ] BookingCancelledEvent received (SAGA compensation) — PNR={Pnr}",
            msg.PnrCode);

        foreach (var seatId in msg.SeatIds)
        {
            try
            {
                // 1. Release in DB: HELD/CONFIRMED → AVAILABLE
                await _seatService.ReleaseSeatAsync(seatId);

                // 2. Remove Redis hold key
                await _redisService.ReleaseSeatHoldAsync(seatId);

                _logger.LogInformation(
                    "[SAGA] Seat {SeatId} released (compensation) — PNR={Pnr}",
                    seatId, msg.PnrCode);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(
                    "[SAGA] Seat {SeatId} not found during release: {Msg}", seatId, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SAGA] Error releasing Seat {SeatId} for PNR={Pnr}", seatId, msg.PnrCode);
                throw;
            }
        }
    }
}
