using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyBooker.NotificationService.DTOs;
using SkyBooker.NotificationService.Services;

namespace SkyBooker.NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Send a single notification — App, Email, or SMS channel</summary>
    [HttpPost("send")]
    [Authorize]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequestDto dto)
    {
        try
        {
            var result = await _notificationService.SendAsync(dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Send booking confirmation with e-ticket PDF attached via MailKit</summary>
    [HttpPost("booking-confirmation")]
    [Authorize]
    public async Task<IActionResult> SendBookingConfirmation(
        [FromBody] BookingConfirmationRequestDto dto)
    {
        try
        {
            await _notificationService.SendBookingConfirmationAsync(dto);
            return Ok(new { message = "Booking confirmation sent via App, Email, and SMS." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Send bulk notification to multiple recipients (Admin / Airline Staff)</summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,AirlineStaff")]
    public async Task<IActionResult> SendBulk([FromBody] SendBulkNotificationRequestDto dto)
    {
        if (dto.RecipientIds == null || dto.RecipientIds.Count == 0)
            return BadRequest(new { message = "At least one recipient ID is required." });

        var results = await _notificationService.SendBulkAsync(dto);
        return Ok(new
        {
            message = $"Bulk notification sent to {results.Count} recipients.",
            notifications = results
        });
    }

    /// <summary>Get all notifications for a recipient</summary>
    [HttpGet("recipient/{recipientId:int}")]
    [Authorize]
    public async Task<IActionResult> GetByRecipient(int recipientId)
    {
        var result = await _notificationService.GetByRecipientAsync(recipientId);
        return Ok(result);
    }

    /// <summary>Get unread notification count for a recipient</summary>
    [HttpGet("recipient/{recipientId:int}/unread-count")]
    [Authorize]
    public async Task<IActionResult> GetUnreadCount(int recipientId)
    {
        var count = await _notificationService.GetUnreadCountAsync(recipientId);
        return Ok(new { recipientId, unreadCount = count });
    }

    /// <summary>Mark a single notification as read</summary>
    [HttpPut("{notificationId:int}/read")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        try
        {
            var result = await _notificationService.MarkAsReadAsync(notificationId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Mark all notifications as read for a recipient</summary>
    [HttpPut("recipient/{recipientId:int}/mark-all-read")]
    [Authorize]
    public async Task<IActionResult> MarkAllRead(int recipientId)
    {
        await _notificationService.MarkAllReadAsync(recipientId);
        return Ok(new { message = "All notifications marked as read." });
    }

    /// <summary>Delete a notification</summary>
    [HttpDelete("{notificationId:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int notificationId)
    {
        try
        {
            await _notificationService.DeleteNotificationAsync(notificationId);
            return Ok(new { message = "Notification deleted." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Get all notifications — Admin only</summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _notificationService.GetAllAsync();
        return Ok(result);
    }
}
