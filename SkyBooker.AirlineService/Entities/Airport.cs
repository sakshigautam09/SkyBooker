using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SkyBooker.AirlineService.Entities;

[Table("airports")]
[Index(nameof(IataCode), IsUnique = true)]
public class Airport
{
    [Key]
    [Column("airport_id")]
    public int AirportId { get; set; }

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

    [Column("city")]
    public string? City { get; set; }

    [Column("country")]
    public string? Country { get; set; }

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("timezone")]
    public string? Timezone { get; set; }

    // Many-to-many with Airline via AirlineAirport join table
    public ICollection<AirlineAirport> AirlineAirports { get; set; } = new List<AirlineAirport>();
}
