using MassTransit;
using SkyBooker.BookingService.Messaging.Events;

namespace SkyBooker.BookingService.Saga;

/// <summary>
/// SAGA State Machine for the booking workflow.
///
/// State flow:
///   Initial → BookingCreated → (PaymentCompleted → Confirmed)
///                            → (PaymentFailed    → Cancelled)
///
/// On cancellation: publishes BookingCancelledEvent as compensation
/// so Seat-Service releases seats and Flight-Service restores AvailableSeats.
/// </summary>
public class BookingSagaStateMachine : MassTransitStateMachine<BookingSagaState>
{
    // ── States ────────────────────────────────────────────────────────────────
    public State BookingCreated { get; private set; } = null!;
    public State Confirmed { get; private set; } = null!;
    public State Cancelled { get; private set; } = null!;

    // ── Events ────────────────────────────────────────────────────────────────
    public Event<BookingCreatedEvent> OnBookingCreated { get; private set; } = null!;
    public Event<PaymentCompletedEvent> OnPaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailedEvent> OnPaymentFailed { get; private set; } = null!;
    public Event<BookingCancelledEvent> OnBookingCancelled { get; private set; } = null!;

    public BookingSagaStateMachine()
    {
        // CorrelationId is the BookingId (as Guid)
        InstanceState(x => x.CurrentState);

        // Correlate events by BookingId
        Event(() => OnBookingCreated,
            x => x.CorrelateBy<string>(s => s.BookingId, ctx => ctx.Message.BookingId)
                  .SelectId(_ => NewId.NextGuid()));

        Event(() => OnPaymentCompleted,
            x => x.CorrelateBy<string>(s => s.BookingId, ctx => ctx.Message.BookingId));

        Event(() => OnPaymentFailed,
            x => x.CorrelateBy<string>(s => s.BookingId, ctx => ctx.Message.BookingId));

        Event(() => OnBookingCancelled,
            x => x.CorrelateBy<string>(s => s.BookingId, ctx => ctx.Message.BookingId));

        // ── Initial → BookingCreated ──────────────────────────────────────────
        Initially(
            When(OnBookingCreated)
                .Then(ctx =>
                {
                    var msg = ctx.Message;
                    ctx.Saga.BookingId = msg.BookingId;
                    ctx.Saga.PnrCode = msg.PnrCode;
                    ctx.Saga.UserId = msg.UserId;
                    ctx.Saga.FlightId = msg.FlightId;
                    ctx.Saga.SeatIds = msg.SeatIds;
                    ctx.Saga.ContactEmail = msg.ContactEmail;
                    ctx.Saga.TotalFare = msg.TotalFare;
                    ctx.Saga.CreatedAt = msg.BookedAt;
                })
                .TransitionTo(BookingCreated));

        // ── BookingCreated → Confirmed (payment success) ──────────────────────
        During(BookingCreated,
            When(OnPaymentCompleted)
                .Then(ctx =>
                {
                    ctx.Saga.PaymentId = ctx.Message.PaymentId;
                    ctx.Saga.ConfirmedAt = ctx.Message.PaidAt;
                })
                .Publish(ctx => new BookingConfirmedEvent
                {
                    BookingId = ctx.Saga.BookingId,
                    PnrCode = ctx.Saga.PnrCode,
                    UserId = ctx.Saga.UserId,
                    FlightId = ctx.Saga.FlightId,
                    SeatIds = ctx.Saga.SeatIds,
                    PaymentId = ctx.Message.PaymentId,
                    TotalFare = ctx.Saga.TotalFare,
                    ContactEmail = ctx.Saga.ContactEmail,
                    ConfirmedAt = ctx.Message.PaidAt
                })
                .TransitionTo(Confirmed)
                .Finalize(),

            // ── BookingCreated → Cancelled (payment failed) ───────────────────
            When(OnPaymentFailed)
                .Then(ctx => ctx.Saga.CancelledAt = ctx.Message.FailedAt)
                .Publish(ctx => new BookingCancelledEvent
                {
                    BookingId = ctx.Saga.BookingId,
                    PnrCode = ctx.Saga.PnrCode,
                    UserId = ctx.Saga.UserId,
                    FlightId = ctx.Saga.FlightId,
                    SeatIds = ctx.Saga.SeatIds,
                    ContactEmail = ctx.Saga.ContactEmail,
                    CancelledAt = ctx.Message.FailedAt
                })
                .TransitionTo(Cancelled)
                .Finalize(),

            // ── Manual cancellation by user ───────────────────────────────────
            When(OnBookingCancelled)
                .Then(ctx => ctx.Saga.CancelledAt = ctx.Message.CancelledAt)
                .TransitionTo(Cancelled)
                .Finalize());

        SetCompletedWhenFinalized();
    }
}
