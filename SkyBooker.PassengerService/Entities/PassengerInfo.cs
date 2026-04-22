using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyBooker.PassengerService.Enums;

namespace SkyBooker.PassengerService.Entities;

[Table("passengers")]
public class PassengerInfo
{
    [Key]
    [Column("passenger_id")]
    public int PassengerId { get; set; }

    [Required]
    [Column("booking_id")]
    public string BookingId { get; set; } = string.Empty;

    [Required]
    [Column("title")]
    public string Title { get; set; } = string.Empty;  // Mr / Mrs / Ms / Dr

    [Required]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Column("date_of_birth")]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [Column("gender")]
    public string Gender { get; set; } = string.Empty;  // Male / Female / Other

    [Required]
    [Column("passport_number")]
    public string PassportNumber { get; set; } = string.Empty;

    [Required]
    [Column("nationality")]
    public string Nationality { get; set; } = string.Empty;

    [Required]
    [Column("passport_expiry")]
    public DateTime PassportExpiry { get; set; }

    [Column("seat_id")]
    public int? SeatId { get; set; }

    [Column("seat_number")]
    public string? SeatNumber { get; set; }

    [Column("ticket_number")]
    public string? TicketNumber { get; set; }

    [Column("passenger_type")]
    public PassengerType PassengerType { get; set; } = PassengerType.Adult;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
