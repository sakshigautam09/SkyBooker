namespace SkyBooker.NotificationService.DTOs;

public class SendBulkNotificationRequestDto
{
    public List<int> RecipientIds { get; set; } = new();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = "App";
    public string? RelatedBookingId { get; set; }
}
