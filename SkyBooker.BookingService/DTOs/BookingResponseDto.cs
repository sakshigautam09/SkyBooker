namespace SkyBooker.BookingService.DTOs;

public class BookingResponseDto
{
    public string BookingId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public int? ReturnFlightId { get; set; }
    public string PnrCode { get; set; } = string.Empty;
    public string TripType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal BaseFare { get; set; }
    public decimal Taxes { get; set; }
    public decimal AncillaryCost { get; set; }
    public decimal TotalFare { get; set; }
    public string? MealPreference { get; set; }
    public int LuggageKg { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? PaymentId { get; set; }
    public DateTime BookedAt { get; set; }
    public List<int> SeatIds { get; set; } = new();
}
