using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SkyBooker.AirlineService.Entities;

[Table("airlines")]
[Index(nameof(IataCode), IsUnique = true)]
public class Airline
{
    [Key]
    [Column("airline_id")]
    public int AirlineId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("iata_code")]
    [StringLength(3)]
    public string IataCode { get; set; } = string.Empty;

    [Column("icao_code")]
    [StringLength(4)]
    public string? IcaoCode { get; set; }

    [Column("logo_url")]
    public string? LogoUrl { get; set; }

    [Column("country")]
    public string? Country { get; set; }

    [Column("contact_email")]
    public string? ContactEmail { get; set; }

    [Column("contact_phone")]
    public string? ContactPhone { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Many-to-many with Airport via AirlineAirport join table
    public ICollection<AirlineAirport> AirlineAirports { get; set; } = new List<AirlineAirport>();
}
