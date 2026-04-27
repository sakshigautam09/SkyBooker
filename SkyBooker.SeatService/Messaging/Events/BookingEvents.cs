namespace SkyBooker.SeatService.Messaging.Events;

/// <summary>
/// Published by Booking-Service when a booking is created.
/// Seat-Service: holds all seats in SeatIds list in Redis + DB.
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
/// Published by Booking-Service when payment is confirmed.
/// Seat-Service: moves all SeatIds from HELD → CONFIRMED, removes Redis keys.
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
/// Published by Booking-Service on cancellation or payment failure.
/// SAGA compensation — Seat-Service: releases all SeatIds back to AVAILABLE,
/// removes Redis hold keys.
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
