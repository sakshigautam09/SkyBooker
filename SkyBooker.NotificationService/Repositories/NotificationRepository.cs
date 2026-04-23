using Microsoft.EntityFrameworkCore;
using SkyBooker.NotificationService.Context;
using SkyBooker.NotificationService.Entities;
using SkyBooker.NotificationService.Enums;

namespace SkyBooker.NotificationService.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<IList<Notification>> FindByRecipientIdAsync(int recipientId)
        => await _context.Notifications
            .Where(n => n.RecipientId == recipientId)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();

    public async Task<IList<Notification>> FindByRecipientIdAndIsReadAsync(
        int recipientId, bool isRead)
        => await _context.Notifications
            .Where(n => n.RecipientId == recipientId && n.IsRead == isRead)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();

    public async Task<int> CountByRecipientIdAndIsReadAsync(int recipientId, bool isRead)
        => await _context.Notifications
            .CountAsync(n => n.RecipientId == recipientId && n.IsRead == isRead);

    public async Task<IList<Notification>> FindByTypeAsync(string type)
    {
        if (!Enum.TryParse<NotificationType>(type, true, out var parsedType))
            return new List<Notification>();

        return await _context.Notifications
            .Where(n => n.Type == parsedType)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();
    }

    public async Task<IList<Notification>> FindByRelatedBookingIdAsync(string bookingId)
        => await _context.Notifications
            .Where(n => n.RelatedBookingId == bookingId)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();

    public async Task DeleteByNotificationIdAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<Notification> UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<Notification?> FindByNotificationIdAsync(int notificationId)
        => await _context.Notifications.FindAsync(notificationId);

    public async Task<IList<Notification>> FindAllAsync()
        => await _context.Notifications
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();

    public async Task UpdateRangeAsync(IList<Notification> notifications)
    {
        _context.Notifications.UpdateRange(notifications);
        await _context.SaveChangesAsync();
    }
}
