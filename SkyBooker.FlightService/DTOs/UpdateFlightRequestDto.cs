namespace SkyBooker.FlightService.DTOs;

public class UpdateFlightRequestDto
{
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public string? AircraftType { get; set; }
    public decimal? BasePrice { get; set; }
}
