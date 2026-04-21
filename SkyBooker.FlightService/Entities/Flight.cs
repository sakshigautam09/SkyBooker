using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SkyBooker.FlightService.Enums;

namespace SkyBooker.FlightService.Entities;

[Table("flights")]
[Index(nameof(FlightNumber), IsUnique = true)]
[Index(nameof(OriginAirportCode), nameof(DestinationAirportCode), nameof(DepartureTime))]
public class Flight
{
    [Key]
    [Column("flight_id")]
    public int FlightId { get; set; }

    [Required]
    [Column("flight_number")]
    public string FlightNumber { get; set; } = string.Empty;

    [Required]
    [Column("airline_id")]
    public int AirlineId { get; set; }

    [Required]
    [Column("origin_airport_code")]
    public string OriginAirportCode { get; set; } = string.Empty;

    [Required]
    [Column("destination_airport_code")]
    public string DestinationAirportCode { get; set; } = string.Empty;

    [Required]
    [Column("departure_time")]
    public DateTime DepartureTime { get; set; }

    [Required]
    [Column("arrival_time")]
    public DateTime ArrivalTime { get; set; }

    [Column("duration_minutes")]
    public int DurationMinutes { get; set; }

    [Column("status")]
    public FlightStatus Status { get; set; } = FlightStatus.Scheduled;

    [Column("aircraft_type")]
    public string AircraftType { get; set; } = string.Empty;

    [Column("total_seats")]
    public int TotalSeats { get; set; }

    [Column("available_seats")]
    public int AvailableSeats { get; set; }

    [Column("base_price")]
    public decimal BasePrice { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
