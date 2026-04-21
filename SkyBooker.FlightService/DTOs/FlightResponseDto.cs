namespace SkyBooker.FlightService.DTOs;

public class FlightResponseDto
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public int AirlineId { get; set; }
    public string OriginAirportCode { get; set; } = string.Empty;
    public string DestinationAirportCode { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AircraftType { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal BasePrice { get; set; }
}
