namespace SkyBooker.BookingService.Messaging.Events;

// ── Published by Booking Service ─────────────────────────────────────────────

/// <summary>
/// Published when a new booking is created (status = Pending).
/// Consumed by: Seat-Service (hold seats), Flight-Service (decrement seats),
///              Notification-Service (send pending confirmation).
/// </summary>
public record BookingCreatedEvent
{
    public string BookingId { get; init; } = string.Empty;
    public string PnrCode { get; init; } = string.Empty;
    public int UserId { get; init; }
    public int FlightId { get; init; }
    public int? ReturnFlightId { get; init; }
    public string TripType { get; init; } = string.Empty;
    public List<int> SeatIds { get; init; } = new();
    public decimal TotalFare { get; init; }
    public string ContactEmail { get; init; } = string.Empty;
    public string? ContactPhone { get; init; }
    public string? MealPreference { get; init; }
    public int LuggageKg { get; init; }
    public DateTime BookedAt { get; init; }
}

/// <summary>
/// Published when a booking is confirmed (payment successful).
/// Consumed by: Seat-Service (confirm seats HELD→CONFIRMED),
///              Notification-Service (send e-ticket email).
/// </summary>
public record BookingConfirmedEvent
{
    public string BookingId { get; init; } = string.Empty;
    public string PnrCode { get; init; } = string.Empty;
    public int UserId { get; init; }
    public int FlightId { get; init; }
    public List<int> SeatIds { get; init; } = new();
    public string PaymentId { get; init; } = string.Empty;
    public decimal TotalFare { get; init; }
    public string ContactEmail { get; init; } = string.Empty;
    public DateTime ConfirmedAt { get; init; }
}

/// <summary>
/// Published when a booking is cancelled.
/// SAGA compensation: Seat-Service releases seats, Flight-Service increments seats,
///                    Notification-Service sends cancellation alert.
/// </summary>
public record BookingCancelledEvent
{
    public string BookingId { get; init; } = string.Empty;
    public string PnrCode { get; init; } = string.Empty;
    public int UserId { get; init; }
    public int FlightId { get; init; }
    public List<int> SeatIds { get; init; } = new();
    public string ContactEmail { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
}

// ── Consumed by Booking Service ───────────────────────────────────────────────

/// <summary>
/// Published by Payment-Service after webhook confirms payment.
/// Booking-Service consumes this to move status Pending → Confirmed.
/// </summary>
public record PaymentCompletedEvent
{
    public string BookingId { get; init; } = string.Empty;
    public string PaymentId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime PaidAt { get; init; }
}

/// <summary>
/// Published by Payment-Service when payment fails or times out.
/// SAGA compensation: Booking-Service moves status to Cancelled,
///                    then publishes BookingCancelledEvent.
/// </summary>
public record PaymentFailedEvent
{
    public string BookingId { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; }
}
