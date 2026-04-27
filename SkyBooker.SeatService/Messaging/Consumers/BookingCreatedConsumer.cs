using MassTransit;
using SkyBooker.SeatService.Messaging.Events;
using SkyBooker.SeatService.Services;
using SkyBooker.SeatService.Services.Redis;

namespace SkyBooker.SeatService.Messaging.Consumers;

/// <summary>
/// Consumes BookingCreatedEvent from Booking-Service.
/// Holds each seat in SeatIds list: sets Redis TTL + updates DB status to HELD.
/// </summary>
public class BookingCreatedConsumer : IConsumer<BookingCreatedEvent>
{
    private readonly ISeatService _seatService;
    private readonly ISeatHoldRedisService _redisService;
    private readonly ILogger<BookingCreatedConsumer> _logger;

    public BookingCreatedConsumer(
        ISeatService seatService,
        ISeatHoldRedisService redisService,
        ILogger<BookingCreatedConsumer> logger)
    {
        _seatService = seatService;
        _redisService = redisService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BookingCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "[RabbitMQ] BookingCreatedEvent received — PNR={Pnr}, Seats={Seats}",
            msg.PnrCode, string.Join(",", msg.SeatIds));

        foreach (var seatId in msg.SeatIds)
        {
            try
            {
                // 1. Hold in DB (status = HELD)
                await _seatService.HoldSeatAsync(seatId, msg.UserId);

                // 2. Hold in Redis with 15-min TTL
                await _redisService.HoldSeatAsync(seatId, msg.UserId);

                _logger.LogInformation(
                    "[SAGA] Seat {SeatId} held for User {UserId} — PNR={Pnr}",
                    seatId, msg.UserId, msg.PnrCode);
            }
            catch (InvalidOperationException ex)
            {
                // Seat no longer available — log and continue (partial hold)
                _logger.LogWarning(
                    "[SAGA] Could not hold Seat {SeatId} for PNR={Pnr}: {Msg}",
                    seatId, msg.PnrCode, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SAGA] Error holding Seat {SeatId} for PNR={Pnr}", seatId, msg.PnrCode);
                throw; // MassTransit will retry
            }
        }
    }
}
