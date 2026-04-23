namespace SkyBooker.NotificationService.DTOs;

public class BookingConfirmationRequestDto
{
    public int RecipientId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string? RecipientPhone { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public string PnrCode { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public decimal TotalFare { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
}
