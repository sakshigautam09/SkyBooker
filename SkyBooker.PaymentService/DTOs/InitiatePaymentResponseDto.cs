namespace SkyBooker.PaymentService.DTOs;

public class InitiatePaymentResponseDto
{
    public string PaymentId { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string GatewayOrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = string.Empty;

    // In production this would be the Razorpay order object
    // For evaluation we simulate it
    public string Message { get; set; } = string.Empty;
}
