using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyBooker.SeatService.Enums;

namespace SkyBooker.SeatService.Entities;

[Table("seats")]
public class Seat
{
    [Key]
    [Column("seat_id")]
    public int SeatId { get; set; }

    [Required]
    [Column("flight_id")]
    public int FlightId { get; set; }

    [Required]
    [Column("seat_number")]
    public string SeatNumber { get; set; } = string.Empty;

    [Column("seat_class")]
    public SeatClass SeatClass { get; set; } = SeatClass.Economy;

    [Column("row")]
    public int Row { get; set; }

    [Column("column")]
    public string Column { get; set; } = string.Empty;

    [Column("is_window")]
    public bool IsWindow { get; set; }

    [Column("is_aisle")]
    public bool IsAisle { get; set; }

    [Column("has_extra_legroom")]
    public bool HasExtraLegroom { get; set; }

    // ConcurrencyToken — EF Core will throw DbUpdateConcurrencyException
    // if two users try to update Status simultaneously
    [ConcurrencyCheck]
    [Column("status")]
    public SeatStatus Status { get; set; } = SeatStatus.Available;

    [Column("price_multiplier")]
    public decimal PriceMultiplier { get; set; } = 1.0m;

    [Column("held_since")]
    public DateTime? HeldSince { get; set; }

    [Column("held_by_user_id")]
    public int? HeldByUserId { get; set; }
}
