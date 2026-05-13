using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkyBooker.PaymentService.DTOs;
using SkyBooker.PaymentService.Entities;
using SkyBooker.PaymentService.Enums;
using SkyBooker.PaymentService.Repositories;

namespace SkyBooker.PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // QuestPDF community license for open source / evaluation use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<InitiatePaymentResponseDto> InitiatePaymentAsync(InitiatePaymentRequestDto dto)
    {
        if (!Enum.TryParse<PaymentMode>(dto.PaymentMode, true, out var paymentMode))
            throw new ArgumentException($"Invalid payment mode: {dto.PaymentMode}. Must be Card, Upi, NetBanking, or Wallet.");

        // Check if payment already exists for this booking
        var existing = await _paymentRepository.FindByBookingIdAsync(dto.BookingId);
        if (existing != null && existing.Status == PaymentStatus.Paid)
            throw new InvalidOperationException("Payment already completed for this booking.");

        // Simulate Razorpay order creation
        var gatewayOrderId = $"order_{Guid.NewGuid().ToString("N")[..16].ToUpper()}";

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid().ToString(),
            BookingId = dto.BookingId,
            UserId = dto.UserId,
            Amount = dto.Amount,
            Currency = dto.Currency,
            Status = PaymentStatus.Pending,
            PaymentMode = paymentMode,
            GatewayOrderId = gatewayOrderId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _paymentRepository.CreateAsync(payment);

        _logger.LogInformation(
            "Payment initiated: PaymentId={PaymentId}, BookingId={BookingId}, Amount={Amount}",
            created.PaymentId, dto.BookingId, dto.Amount);

        return new InitiatePaymentResponseDto
        {
            PaymentId = created.PaymentId,
            BookingId = created.BookingId,
            Amount = created.Amount,
            Currency = created.Currency,
            GatewayOrderId = gatewayOrderId,
            Status = created.Status.ToString(),
            PaymentMode = created.PaymentMode.ToString(),
            Message = $"Payment initiated. Use GatewayOrderId '{gatewayOrderId}' to complete payment via Razorpay."
        };
    }

    public async Task<PaymentResponseDto> ProcessPaymentAsync(WebhookPayloadDto dto)
    {
        var payment = await _paymentRepository.FindByPaymentIdAsync(dto.PaymentId)
            ?? throw new KeyNotFoundException($"Payment {dto.PaymentId} not found.");

        // HMAC signature verification
        var isValidSignature = VerifyHmacSignature(
            dto.GatewayOrderId, dto.TransactionId, dto.RazorpaySignature);

        if (!isValidSignature)
        {
            _logger.LogWarning("Invalid HMAC signature for PaymentId={PaymentId}", dto.PaymentId);
            throw new UnauthorizedAccessException("Invalid payment gateway signature.");
        }

        if (dto.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = PaymentStatus.Paid;
            payment.TransactionId = dto.TransactionId;
            payment.GatewayResponse = dto.GatewayResponse;
            payment.PaidAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Payment successful: PaymentId={PaymentId}, TransactionId={TransactionId}",
                dto.PaymentId, dto.TransactionId);

            // Notify Booking Service to update status to Confirmed
            await NotifyBookingServiceAsync(payment.BookingId, "Confirmed", payment.PaymentId);
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.GatewayResponse = dto.GatewayResponse;

            _logger.LogWarning("Payment failed: PaymentId={PaymentId}", dto.PaymentId);
        }

        var updated = await _paymentRepository.UpdateAsync(payment);
        return MapToDto(updated);
    }

    // ── NEW: Simulate payment success for evaluation/dev (no real Razorpay webhook needed) ──
    public async Task<PaymentResponseDto> SimulatePaymentSuccessAsync(string paymentId)
    {
        var payment = await _paymentRepository.FindByPaymentIdAsync(paymentId)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        if (payment.Status == PaymentStatus.Paid)
            throw new InvalidOperationException("Payment is already marked as paid.");

        var fakeTransactionId = $"sim_txn_{Guid.NewGuid().ToString("N")[..12].ToUpper()}";

        payment.Status = PaymentStatus.Paid;
        payment.TransactionId = fakeTransactionId;
        payment.GatewayResponse = "Simulated payment success";
        payment.PaidAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Simulated payment success: PaymentId={PaymentId}, BookingId={BookingId}",
            paymentId, payment.BookingId);

        var updated = await _paymentRepository.UpdateAsync(payment);

        // Notify BookingService → sets booking status to Confirmed
        await NotifyBookingServiceAsync(payment.BookingId, "Confirmed", payment.PaymentId);

        return MapToDto(updated);
    }

    public async Task<PaymentResponseDto?> GetPaymentByBookingAsync(string bookingId)
    {
        var payment = await _paymentRepository.FindByBookingIdAsync(bookingId);
        return payment == null ? null : MapToDto(payment);
    }

    public async Task<IList<PaymentResponseDto>> GetPaymentsByUserAsync(int userId)
    {
        var payments = await _paymentRepository.FindByUserIdAsync(userId);
        return payments.Select(MapToDto).ToList();
    }

    public async Task<PaymentResponseDto?> GetPaymentStatusAsync(string paymentId)
    {
        var payment = await _paymentRepository.FindByPaymentIdAsync(paymentId);
        return payment == null ? null : MapToDto(payment);
    }

    public async Task<PaymentResponseDto> RefundPaymentAsync(string paymentId, RefundRequestDto dto)
    {
        var payment = await _paymentRepository.FindByPaymentIdAsync(paymentId)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        if (payment.Status != PaymentStatus.Paid)
            throw new InvalidOperationException("Only paid payments can be refunded.");

        var refundAmount = dto.RefundAmount ?? payment.Amount;

        if (refundAmount > payment.Amount)
            throw new InvalidOperationException("Refund amount cannot exceed the paid amount.");

        // In production: call Razorpay refund API
        payment.Status = PaymentStatus.Refunded;
        payment.RefundAmount = refundAmount;
        payment.RefundedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Payment refunded: PaymentId={PaymentId}, Amount={Amount}",
            paymentId, refundAmount);

        var updated = await _paymentRepository.UpdateAsync(payment);
        return MapToDto(updated);
    }

    public async Task UpdatePaymentStatusAsync(string paymentId, string status)
    {
        var payment = await _paymentRepository.FindByPaymentIdAsync(paymentId)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        if (!Enum.TryParse<PaymentStatus>(status, true, out var newStatus))
            throw new ArgumentException($"Invalid payment status: {status}");

        payment.Status = newStatus;
        await _paymentRepository.UpdateAsync(payment);
    }

    public async Task<FileContentResult> GenerateReceiptAsync(string paymentId)
    {
        var payment = await _paymentRepository.FindByPaymentIdAsync(paymentId)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        if (payment.Status != PaymentStatus.Paid)
            throw new InvalidOperationException("Receipt is only available for paid payments.");

        var pdfBytes = GenerateReceiptPdf(payment);

        return new FileContentResult(pdfBytes, "application/pdf")
        {
            FileDownloadName = $"receipt_{payment.PaymentId[..8]}.pdf"
        };
    }

    public async Task<decimal> GetRevenueAsync(DateTime start, DateTime end)
    {
        var payments = await _paymentRepository.FindByPaidAtBetweenAsync(start, end);
        return payments.Sum(p => p.Amount);
    }

    // ── Private Helpers ────────────────────────────────────────────────────────

    private bool VerifyHmacSignature(string orderId, string transactionId, string signature)
    {
        var keySecret = _configuration["Razorpay:KeySecret"] ?? string.Empty;

        if (string.IsNullOrEmpty(keySecret) || keySecret == "YOUR_RAZORPAY_KEY_SECRET")
        {
            _logger.LogWarning("Razorpay keys not configured — skipping HMAC verification for evaluation.");
            return true;
        }

        var payload = $"{orderId}|{transactionId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keySecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expectedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

        return expectedSignature == signature.ToLower();
    }

    private async Task NotifyBookingServiceAsync(
        string bookingId, string status, string paymentId)
    {
        try
        {
            var bookingServiceUrl = _configuration["ServiceUrls:BookingService"];
            var client = _httpClientFactory.CreateClient();

            var payload = JsonSerializer.Serialize(new
            {
                status,
                paymentId
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PutAsync(
                $"{bookingServiceUrl}/api/bookings/{bookingId}/status", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Booking {BookingId} status updated to {Status}", bookingId, status);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to update booking {BookingId} status. Response: {StatusCode}",
                    bookingId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error notifying Booking Service for BookingId={BookingId}", bookingId);
        }
    }

    private static byte[] GenerateReceiptPdf(Payment payment)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Column(col =>
                {
                    col.Item().Text("SkyBooker")
                        .FontSize(28).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Payment Receipt")
                        .FontSize(16).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Payment ID").Bold();
                            left.Item().Text(payment.PaymentId).FontSize(10)
                                .FontColor(Colors.Grey.Darken2);
                        });
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("Date").Bold();
                            right.Item().Text(payment.PaidAt?.ToString("dd MMM yyyy HH:mm") ?? "-");
                        });
                    });

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2)
                                .Padding(8).Text("Description").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Blue.Darken2)
                                .Padding(8).Text("Amount").Bold().FontColor(Colors.White);
                        });

                        table.Cell().Padding(8).Text($"Booking ID: {payment.BookingId[..8]}...");
                        table.Cell().Padding(8).Text($"{payment.Currency} {payment.Amount:F2}");

                        table.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(8).Text("Payment Mode");
                        table.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(8).Text(payment.PaymentMode.ToString());

                        table.Cell().Padding(8).Text("Transaction ID");
                        table.Cell().Padding(8).Text(payment.TransactionId ?? "N/A")
                            .FontSize(10);

                        table.Cell().Background(Colors.Green.Lighten4)
                            .Padding(8).Text("Status").Bold();
                        table.Cell().Background(Colors.Green.Lighten4)
                            .Padding(8).Text(payment.Status.ToString()).Bold()
                            .FontColor(Colors.Green.Darken3);
                    });

                    if (payment.RefundAmount > 0)
                    {
                        col.Item().PaddingTop(15).Background(Colors.Orange.Lighten4)
                            .Padding(10).Row(row =>
                            {
                                row.RelativeItem().Text("Refund Amount").Bold();
                                row.AutoItem().Text($"{payment.Currency} {payment.RefundAmount:F2}").Bold();
                            });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Generated by SkyBooker Platform • ").FontSize(10)
                        .FontColor(Colors.Grey.Medium);
                    text.Span(DateTime.UtcNow.ToString("dd MMM yyyy")).FontSize(10)
                        .FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    private static PaymentResponseDto MapToDto(Payment p) => new()
    {
        PaymentId = p.PaymentId,
        BookingId = p.BookingId,
        UserId = p.UserId,
        Amount = p.Amount,
        Currency = p.Currency,
        Status = p.Status.ToString(),
        PaymentMode = p.PaymentMode.ToString(),
        GatewayOrderId = p.GatewayOrderId,
        TransactionId = p.TransactionId,
        GatewayResponse = p.GatewayResponse,
        PaidAt = p.PaidAt,
        RefundedAt = p.RefundedAt,
        RefundAmount = p.RefundAmount,
        CreatedAt = p.CreatedAt
    };
}