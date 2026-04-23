namespace SkyBooker.PaymentService.DTOs;

public class RefundRequestDto
{
    public decimal? RefundAmount { get; set; } // null = full refund
    public string Reason { get; set; } = string.Empty;
}
