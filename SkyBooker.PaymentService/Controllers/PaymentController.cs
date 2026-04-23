using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.PaymentService.DTOs;
using SkyBooker.PaymentService.Services;

namespace SkyBooker.PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>Initiate a payment — creates pending record and returns gateway order ID</summary>
    [HttpPost("initiate")]
    [Authorize]
    public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequestDto dto)
    {
        try
        {
            var result = await _paymentService.InitiatePaymentAsync(dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Webhook endpoint — receives Razorpay/Stripe callback,
    /// verifies HMAC signature, updates payment status, notifies Booking Service
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> ProcessWebhook([FromBody] WebhookPayloadDto dto)
    {
        try
        {
            var result = await _paymentService.ProcessPaymentAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>Initiate a refund for a paid payment</summary>
    [HttpPost("{paymentId}/refund")]
    [Authorize]
    public async Task<IActionResult> RefundPayment(
        string paymentId, [FromBody] RefundRequestDto dto)
    {
        try
        {
            var result = await _paymentService.RefundPaymentAsync(paymentId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Get payment by booking ID</summary>
    [HttpGet("booking/{bookingId}")]
    [Authorize]
    public async Task<IActionResult> GetByBooking(string bookingId)
    {
        var result = await _paymentService.GetPaymentByBookingAsync(bookingId);
        return result == null
            ? NotFound(new { message = "No payment found for this booking." })
            : Ok(result);
    }

    /// <summary>Get all payments for a user</summary>
    [HttpGet("user/{userId:int}")]
    [Authorize]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var result = await _paymentService.GetPaymentsByUserAsync(userId);
        return Ok(result);
    }

    /// <summary>Get payment status by payment ID</summary>
    [HttpGet("{paymentId}/status")]
    [Authorize]
    public async Task<IActionResult> GetStatus(string paymentId)
    {
        var result = await _paymentService.GetPaymentStatusAsync(paymentId);
        return result == null
            ? NotFound(new { message = "Payment not found." })
            : Ok(new { paymentId = result.PaymentId, status = result.Status, paidAt = result.PaidAt });
    }

    /// <summary>Update payment status manually (Admin only)</summary>
    [HttpPut("{paymentId}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(string paymentId, [FromBody] string status)
    {
        try
        {
            await _paymentService.UpdatePaymentStatusAsync(paymentId, status);
            return Ok(new { message = "Payment status updated." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Download PDF receipt for a paid payment</summary>
    [HttpGet("{paymentId}/receipt")]
    [Authorize]
    public async Task<IActionResult> GetReceipt(string paymentId)
    {
        try
        {
            var result = await _paymentService.GenerateReceiptAsync(paymentId);
            return result;
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Get total revenue between two dates (Admin only)</summary>
    [HttpGet("revenue")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end)
    {
        if (start > end)
            return BadRequest(new { message = "Start date must be before end date." });

        var revenue = await _paymentService.GetRevenueAsync(start, end);
        return Ok(new
        {
            start,
            end,
            totalRevenue = revenue,
            currency = "INR"
        });
    }
}
