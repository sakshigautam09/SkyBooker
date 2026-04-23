using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyBooker.PaymentService.Enums;

namespace SkyBooker.PaymentService.Entities;

[Table("payments")]
public class Payment
{
    [Key]
    [Column("payment_id")]
    public string PaymentId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("booking_id")]
    public string BookingId { get; set; } = string.Empty;

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "INR";

    [Column("status")]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    [Column("payment_mode")]
    public PaymentMode PaymentMode { get; set; } = PaymentMode.Card;

    // Gateway order/session ID returned by Razorpay/Stripe
    [Column("gateway_order_id")]
    public string? GatewayOrderId { get; set; }

    // Transaction ID returned after successful payment
    [Column("transaction_id")]
    public string? TransactionId { get; set; }

    // Raw response from the gateway (stored as JSON string)
    [Column("gateway_response")]
    public string? GatewayResponse { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("refunded_at")]
    public DateTime? RefundedAt { get; set; }

    [Column("refund_amount")]
    public decimal RefundAmount { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
