namespace SkyBooker.PaymentService.DTOs;

public class PaymentResponseDto
{
    public string PaymentId { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = string.Empty;
    public string? GatewayOrderId { get; set; }
    public string? TransactionId { get; set; }
    public string? GatewayResponse { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
