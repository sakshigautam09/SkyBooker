using System.ComponentModel.DataAnnotations.Schema;

namespace SkyBooker.AirlineService.Entities;

[Table("airline_airports")]
public class AirlineAirport
{
    [Column("airline_id")]
    public int AirlineId { get; set; }

    [Column("airport_id")]
    public int AirportId { get; set; }

    public Airline Airline { get; set; } = null!;
    public Airport Airport { get; set; } = null!;
}
