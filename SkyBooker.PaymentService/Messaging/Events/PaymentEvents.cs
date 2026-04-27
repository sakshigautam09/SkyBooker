namespace SkyBooker.PaymentService.Messaging.Events;

/// <summary>
/// Published by Payment-Service after webhook confirms payment success.
/// Consumed by:
///   - Booking-Service  → moves booking Pending → Confirmed
///   - Seat-Service     → moves seats HELD → CONFIRMED
///   - Notification-Service → sends e-ticket email
/// </summary>
public record PaymentCompletedEvent
{
    public string PaymentId { get; init; } = string.Empty;
    public string BookingId { get; init; } = string.Empty;
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string PaymentMode { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
    public DateTime PaidAt { get; init; }
}

/// <summary>
/// Published by Payment-Service when webhook reports failure or payment times out.
/// Consumed by:
///   - Booking-Service  → SAGA compensation: cancels booking
///   - Seat-Service     → releases held seats back to AVAILABLE
///   - Notification-Service → sends payment failure alert
/// </summary>
public record PaymentFailedEvent
{
    public string PaymentId { get; init; } = string.Empty;
    public string BookingId { get; init; } = string.Empty;
    public int UserId { get; init; }
    public decimal Amount { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; }
}

/// <summary>
/// Published by Payment-Service when a refund is processed.
/// Consumed by:
///   - Notification-Service → sends refund confirmation
/// </summary>
public record PaymentRefundedEvent
{
    public string PaymentId { get; init; } = string.Empty;
    public string BookingId { get; init; } = string.Empty;
    public int UserId { get; init; }
    public decimal RefundAmount { get; init; }
    public string Currency { get; init; } = "INR";
    public DateTime RefundedAt { get; init; }
}
