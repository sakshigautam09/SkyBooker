using SkyBooker.NotificationService.DTOs;

namespace SkyBooker.NotificationService.Services;

public interface INotificationService
{
    Task<NotificationResponseDto> SendAsync(SendNotificationRequestDto dto);
    Task SendBookingConfirmationAsync(BookingConfirmationRequestDto dto);
    Task<IList<NotificationResponseDto>> SendBulkAsync(SendBulkNotificationRequestDto dto);
    Task<NotificationResponseDto> MarkAsReadAsync(int notificationId);
    Task MarkAllReadAsync(int recipientId);
    Task<IList<NotificationResponseDto>> GetByRecipientAsync(int recipientId);
    Task<int> GetUnreadCountAsync(int recipientId);
    Task DeleteNotificationAsync(int notificationId);
    Task<IList<NotificationResponseDto>> GetAllAsync();
    Task SendEmailAsync(string toEmail, string toName, string subject, string body);
    Task SendSmsAsync(string toPhone, string message);
}
