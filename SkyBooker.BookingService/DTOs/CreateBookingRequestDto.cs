namespace SkyBooker.BookingService.DTOs;

public class CreateBookingRequestDto
{
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public int? ReturnFlightId { get; set; }
    public string TripType { get; set; } = "OneWay";
    public List<int> SeatIds { get; set; } = new();
    public string? MealPreference { get; set; }
    public int LuggageKg { get; set; } = 0;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public decimal BasePrice { get; set; }
}
