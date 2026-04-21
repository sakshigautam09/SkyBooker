namespace SkyBooker.FlightService.DTOs;

public class AddFlightRequestDto
{
    public string FlightNumber { get; set; } = string.Empty;
    public int AirlineId { get; set; }
    public string OriginAirportCode { get; set; } = string.Empty;
    public string DestinationAirportCode { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string AircraftType { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public decimal BasePrice { get; set; }
}
