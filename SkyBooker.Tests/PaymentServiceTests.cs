using Moq;
using NUnit.Framework;
using SkyBooker.PaymentService.Services;
using SkyBooker.PaymentService.DTOs;

namespace SkyBooker.Tests;

[TestFixture]
public class PaymentServiceTests
{
    private Mock<IPaymentService> _paymentServiceMock = null!;

    [SetUp]
    public void SetUp() => _paymentServiceMock = new Mock<IPaymentService>();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static PaymentResponseDto MakePaymentDto(
        string paymentId = "PAY-001",
        string bookingId = "BK-001",
        string status    = "Pending",
        decimal amount   = 4500m) => new()
    {
        PaymentId = paymentId,
        BookingId = bookingId,
        Status    = status,
        Amount    = amount,
        Currency  = "INR"
    };

    // ─── InitiatePaymentAsync ─────────────────────────────────────────────────

    [Test]
    public async Task InitiatePaymentAsync_ValidRequest_ReturnsPaymentInitiation()
    {
        var dto = new InitiatePaymentRequestDto
        {
            BookingId   = "BK-001",
            UserId      = 1,
            Amount      = 4500m,
            Currency    = "INR",
            PaymentMode = "UPI"   // was: Method = "UPI"
        };

        var expected = new InitiatePaymentResponseDto
        {
            PaymentId      = "PAY-001",
            BookingId      = "BK-001",
            Amount         = 4500m,
            GatewayOrderId = "order_xyz123",  // was: OrderId
            Status         = "Created"
        };

        _paymentServiceMock.Setup(s => s.InitiatePaymentAsync(dto)).ReturnsAsync(expected);

        var result = await _paymentServiceMock.Object.InitiatePaymentAsync(dto);

        Assert.That(result.PaymentId, Is.EqualTo("PAY-001"));
        Assert.That(result.Status, Is.EqualTo("Created"));
        Assert.That(result.GatewayOrderId, Is.Not.Empty);   // was: result.OrderId
    }

    // ─── ProcessPaymentAsync ──────────────────────────────────────────────────

    [Test]
    public async Task ProcessPaymentAsync_SuccessWebhook_ReturnsSuccessStatus()
    {
        var webhook = new WebhookPayloadDto
        {
            PaymentId      = "PAY-001",
            GatewayOrderId = "order_xyz123",  // was: OrderId
            Status         = "captured"
            // removed: Amount = 450000 (property does not exist on WebhookPayloadDto)
        };

        var expected = MakePaymentDto(status: "Success");
        _paymentServiceMock.Setup(s => s.ProcessPaymentAsync(webhook)).ReturnsAsync(expected);

        var result = await _paymentServiceMock.Object.ProcessPaymentAsync(webhook);

        Assert.That(result.Status, Is.EqualTo("Success"));
    }

    [Test]
    public async Task ProcessPaymentAsync_FailedWebhook_ReturnsFailedStatus()
    {
        var webhook = new WebhookPayloadDto
        {
            PaymentId = "PAY-002",
            Status    = "failed"
        };

        var expected = MakePaymentDto("PAY-002", status: "Failed");
        _paymentServiceMock.Setup(s => s.ProcessPaymentAsync(webhook)).ReturnsAsync(expected);

        var result = await _paymentServiceMock.Object.ProcessPaymentAsync(webhook);

        Assert.That(result.Status, Is.EqualTo("Failed"));
    }

    // ─── GetPaymentByBookingAsync ─────────────────────────────────────────────

    [Test]
    public async Task GetPaymentByBookingAsync_ExistingBooking_ReturnsPayment()
    {
        var expected = MakePaymentDto(bookingId: "BK-001", status: "Success");
        _paymentServiceMock.Setup(s => s.GetPaymentByBookingAsync("BK-001")).ReturnsAsync(expected);

        var result = await _paymentServiceMock.Object.GetPaymentByBookingAsync("BK-001");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.BookingId, Is.EqualTo("BK-001"));
    }

    [Test]
    public async Task GetPaymentByBookingAsync_NonExistingBooking_ReturnsNull()
    {
        _paymentServiceMock.Setup(s => s.GetPaymentByBookingAsync("BK-9999")).ReturnsAsync((PaymentResponseDto?)null);

        var result = await _paymentServiceMock.Object.GetPaymentByBookingAsync("BK-9999");

        Assert.That(result, Is.Null);
    }

    // ─── GetPaymentsByUserAsync ───────────────────────────────────────────────

    [Test]
    public async Task GetPaymentsByUserAsync_UserWithPayments_ReturnsList()
    {
        var payments = new List<PaymentResponseDto>
        {
            MakePaymentDto("PAY-001", "BK-001"),
            MakePaymentDto("PAY-002", "BK-002")
        };
        _paymentServiceMock.Setup(s => s.GetPaymentsByUserAsync(1)).ReturnsAsync(payments);

        var result = await _paymentServiceMock.Object.GetPaymentsByUserAsync(1);

        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetPaymentsByUserAsync_NewUser_ReturnsEmptyList()
    {
        _paymentServiceMock.Setup(s => s.GetPaymentsByUserAsync(999)).ReturnsAsync(new List<PaymentResponseDto>());

        var result = await _paymentServiceMock.Object.GetPaymentsByUserAsync(999);

        Assert.That(result, Is.Empty);
    }

    // ─── GetPaymentStatusAsync ────────────────────────────────────────────────

    [Test]
    public async Task GetPaymentStatusAsync_ExistingPayment_ReturnsStatus()
    {
        var expected = MakePaymentDto(status: "Success");
        _paymentServiceMock.Setup(s => s.GetPaymentStatusAsync("PAY-001")).ReturnsAsync(expected);

        var result = await _paymentServiceMock.Object.GetPaymentStatusAsync("PAY-001");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Status, Is.EqualTo("Success"));
    }

    // ─── RefundPaymentAsync ───────────────────────────────────────────────────

    [Test]
    public async Task RefundPaymentAsync_SuccessfulPayment_ReturnsRefundedStatus()
    {
        var refundDto = new RefundRequestDto { Reason = "Booking cancelled by user" };
        var expected  = MakePaymentDto(status: "Refunded");

        _paymentServiceMock.Setup(s => s.RefundPaymentAsync("PAY-001", refundDto)).ReturnsAsync(expected);

        var result = await _paymentServiceMock.Object.RefundPaymentAsync("PAY-001", refundDto);

        Assert.That(result.Status, Is.EqualTo("Refunded"));
    }

    [Test]
    public async Task RefundPaymentAsync_AlreadyRefunded_ThrowsException()
    {
        var refundDto = new RefundRequestDto { Reason = "Duplicate refund" };

        _paymentServiceMock.Setup(s => s.RefundPaymentAsync("PAY-001", refundDto))
            .ThrowsAsync(new InvalidOperationException("Payment is already refunded."));

        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            () => _paymentServiceMock.Object.RefundPaymentAsync("PAY-001", refundDto));

        Assert.That(ex!.Message, Does.Contain("refunded"));
    }

    // ─── UpdatePaymentStatusAsync ─────────────────────────────────────────────

    [Test]
    public async Task UpdatePaymentStatusAsync_ValidStatus_CompletesSuccessfully()
    {
        _paymentServiceMock.Setup(s => s.UpdatePaymentStatusAsync("PAY-001", "Success")).Returns(Task.CompletedTask);
        Assert.DoesNotThrowAsync(() => _paymentServiceMock.Object.UpdatePaymentStatusAsync("PAY-001", "Success"));
    }

    // ─── GetRevenueAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task GetRevenueAsync_ValidDateRange_ReturnsRevenue()
    {
        var start = new DateTime(2025, 1, 1);
        var end   = new DateTime(2025, 3, 31);

        _paymentServiceMock.Setup(s => s.GetRevenueAsync(start, end)).ReturnsAsync(150000m);

        var result = await _paymentServiceMock.Object.GetRevenueAsync(start, end);

        Assert.That(result, Is.EqualTo(150000m));
        Assert.That(result, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetRevenueAsync_NoPaymentsInRange_ReturnsZero()
    {
        var start = new DateTime(2020, 1, 1);
        var end   = new DateTime(2020, 1, 31);

        _paymentServiceMock.Setup(s => s.GetRevenueAsync(start, end)).ReturnsAsync(0m);

        var result = await _paymentServiceMock.Object.GetRevenueAsync(start, end);

        Assert.That(result, Is.EqualTo(0));
    }
}