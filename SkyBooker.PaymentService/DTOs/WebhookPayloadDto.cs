namespace SkyBooker.PaymentService.DTOs;

public class WebhookPayloadDto
{
    public string PaymentId { get; set; } = string.Empty;
    public string GatewayOrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;  // paid / failed
    public string RazorpaySignature { get; set; } = string.Empty;
    public string GatewayResponse { get; set; } = string.Empty;
}
