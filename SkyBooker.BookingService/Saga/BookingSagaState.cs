using MassTransit;

namespace SkyBooker.BookingService.Saga;

/// <summary>
/// Persisted state for each booking's SAGA instance.
/// MassTransit tracks this in memory (or a DB if you add saga persistence).
/// </summary>
public class BookingSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = string.Empty;

    public string BookingId { get; set; } = string.Empty;
    public string PnrCode { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public List<int> SeatIds { get; set; } = new();
    public string ContactEmail { get; set; } = string.Empty;
    public decimal TotalFare { get; set; }
    public string? PaymentId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
