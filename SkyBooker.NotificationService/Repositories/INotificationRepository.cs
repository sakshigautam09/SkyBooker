using SkyBooker.NotificationService.Entities;

namespace SkyBooker.NotificationService.Repositories;

public interface INotificationRepository
{
    Task<IList<Notification>> FindByRecipientIdAsync(int recipientId);
    Task<IList<Notification>> FindByRecipientIdAndIsReadAsync(int recipientId, bool isRead);
    Task<int> CountByRecipientIdAndIsReadAsync(int recipientId, bool isRead);
    Task<IList<Notification>> FindByTypeAsync(string type);
    Task<IList<Notification>> FindByRelatedBookingIdAsync(string bookingId);
    Task DeleteByNotificationIdAsync(int notificationId);
    Task<Notification> CreateAsync(Notification notification);
    Task<Notification> UpdateAsync(Notification notification);
    Task<Notification?> FindByNotificationIdAsync(int notificationId);
    Task<IList<Notification>> FindAllAsync();
    Task UpdateRangeAsync(IList<Notification> notifications);
}
