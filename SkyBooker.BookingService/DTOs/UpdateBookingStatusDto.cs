namespace SkyBooker.BookingService.DTOs;

public class UpdateBookingStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? PaymentId { get; set; }
}
