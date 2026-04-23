namespace SkyBooker.PaymentService.DTOs;

public class InitiatePaymentRequestDto
{
    public string BookingId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentMode { get; set; } = "Card";
}
