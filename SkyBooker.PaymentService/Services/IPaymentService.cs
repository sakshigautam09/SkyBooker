using Microsoft.AspNetCore.Mvc;
using SkyBooker.PaymentService.DTOs;

namespace SkyBooker.PaymentService.Services;

public interface IPaymentService
{
    Task<InitiatePaymentResponseDto> InitiatePaymentAsync(InitiatePaymentRequestDto dto);
    Task<PaymentResponseDto> ProcessPaymentAsync(WebhookPayloadDto dto);
    Task<PaymentResponseDto?> GetPaymentByBookingAsync(string bookingId);
    Task<IList<PaymentResponseDto>> GetPaymentsByUserAsync(int userId);
    Task<PaymentResponseDto?> GetPaymentStatusAsync(string paymentId);
    Task<PaymentResponseDto> RefundPaymentAsync(string paymentId, RefundRequestDto dto);
    Task UpdatePaymentStatusAsync(string paymentId, string status);
    Task<FileContentResult> GenerateReceiptAsync(string paymentId);
    Task<decimal> GetRevenueAsync(DateTime start, DateTime end);
}
