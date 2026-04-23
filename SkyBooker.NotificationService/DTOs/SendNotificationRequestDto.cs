namespace SkyBooker.NotificationService.DTOs;

public class SendNotificationRequestDto
{
    public int RecipientId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = "App";
    public string? RelatedBookingId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
}
