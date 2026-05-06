using MassTransit;
using SkyBooker.NotificationService.DTOs;
using SkyBooker.NotificationService.Services;

// ── Namespace MUST match BookingService exactly ────────────────────────────────
namespace SkyBooker.BookingService.Messaging.Events
{
    public record BookingConfirmedEvent
    {
        public string BookingId     { get; init; } = string.Empty;
        public string PnrCode       { get; init; } = string.Empty;
        public int    UserId        { get; init; }
        public int    FlightId      { get; init; }
        public List<int> SeatIds    { get; init; } = new();
        public string PaymentId     { get; init; } = string.Empty;
        public decimal TotalFare    { get; init; }
        public string ContactEmail  { get; init; } = string.Empty;
        public DateTime ConfirmedAt { get; init; }
    }
}

namespace SkyBooker.NotificationService.Consumers
{
    using SkyBooker.BookingService.Messaging.Events;

    public class BookingConfirmedConsumer : IConsumer<BookingConfirmedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<BookingConfirmedConsumer> _logger;

        public BookingConfirmedConsumer(
            INotificationService notificationService,
            ILogger<BookingConfirmedConsumer> logger)
        {
            _notificationService = notificationService;
            _logger              = logger;
        }

        public async Task Consume(ConsumeContext<BookingConfirmedEvent> context)
        {
            var evt = context.Message;

            _logger.LogInformation(
                "[RabbitMQ] BookingConfirmedEvent received — BookingId={Id}, PNR={Pnr}",
                evt.BookingId, evt.PnrCode);

            try
            {
                var dto = new BookingConfirmationRequestDto
                {
                    RecipientId    = evt.UserId,
                    RecipientEmail = evt.ContactEmail,
                    RecipientPhone = null,
                    RecipientName  = evt.ContactEmail,
                    BookingId      = evt.BookingId,
                    PnrCode        = evt.PnrCode,
                    FlightNumber   = evt.FlightId.ToString(),
                    Origin         = string.Empty,
                    Destination    = string.Empty,
                    DepartureTime  = evt.ConfirmedAt,
                    TotalFare      = evt.TotalFare,
                    SeatNumber     = evt.SeatIds.Count > 0
                                        ? string.Join(", ", evt.SeatIds)
                                        : "N/A"
                };

                await _notificationService.SendBookingConfirmationAsync(dto);

                _logger.LogInformation(
                    "[SAGA] Booking confirmation dispatched — PNR={Pnr}", evt.PnrCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SAGA] Failed to send confirmation for PNR={Pnr}", evt.PnrCode);
                throw;
            }
        }
    }
}