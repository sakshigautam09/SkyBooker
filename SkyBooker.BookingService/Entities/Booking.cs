using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyBooker.BookingService.Enums;

namespace SkyBooker.BookingService.Entities;

[Table("bookings")]
public class Booking
{
    [Key]
    [Column("booking_id")]
    public string BookingId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("flight_id")]
    public int FlightId { get; set; }

    // For round trips, this holds the return flight id
    [Column("return_flight_id")]
    public int? ReturnFlightId { get; set; }

    [Required]
    [Column("pnr_code")]
    public string PnrCode { get; set; } = string.Empty;

    [Column("trip_type")]
    public TripType TripType { get; set; } = TripType.OneWay;

    [Column("status")]
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    [Column("base_fare")]
    public decimal BaseFare { get; set; }

    [Column("taxes")]
    public decimal Taxes { get; set; }

    [Column("ancillary_cost")]
    public decimal AncillaryCost { get; set; }

    [Column("total_fare")]
    public decimal TotalFare { get; set; }

    [Column("meal_preference")]
    public string? MealPreference { get; set; }

    [Column("luggage_kg")]
    public int LuggageKg { get; set; } = 0;

    [Required]
    [Column("contact_email")]
    public string ContactEmail { get; set; } = string.Empty;

    [Column("contact_phone")]
    public string? ContactPhone { get; set; }

    [Column("payment_id")]
    public string? PaymentId { get; set; }

    [Column("booked_at")]
    public DateTime BookedAt { get; set; } = DateTime.UtcNow;

    // Seat IDs held/confirmed for this booking
    [Column("seat_ids")]
    public string SeatIds { get; set; } = string.Empty;
}
