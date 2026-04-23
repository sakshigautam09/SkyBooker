namespace SkyBooker.NotificationService.DTOs;

public class NotificationResponseDto
{
    public int NotificationId { get; set; }
    public int RecipientId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string? RelatedBookingId { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
}
