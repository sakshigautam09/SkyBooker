using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkyBooker.NotificationService.Enums;

namespace SkyBooker.NotificationService.Entities;

[Table("notifications")]
public class Notification
{
    [Key]
    [Column("notification_id")]
    public int NotificationId { get; set; }

    [Required]
    [Column("recipient_id")]
    public int RecipientId { get; set; }

    [Column("type")]
    public NotificationType Type { get; set; } = NotificationType.General;

    [Required]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    [Column("channel")]
    public NotificationChannel Channel { get; set; } = NotificationChannel.App;

    // Deep-link to the related booking
    [Column("related_booking_id")]
    public string? RelatedBookingId { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("sent_at")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Recipient email/phone for email and SMS channels
    [Column("recipient_email")]
    public string? RecipientEmail { get; set; }

    [Column("recipient_phone")]
    public string? RecipientPhone { get; set; }
}
