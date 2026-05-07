using Microsoft.EntityFrameworkCore;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Infrastructure.Data;

namespace Notification.Infrastructure.Services;

public class NotificationService(NotificationDbContext db) : INotificationService
{
    public async Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId)
    {
        var notifications = await db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(n => new NotificationResponse(
            n.Id, n.UserId, n.Title, n.Message, n.Type.ToString(), n.IsRead, n.CreatedAt));
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await db.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var notifications = await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in notifications)
        {
            n.IsRead = true;
        }

        await db.SaveChangesAsync();
    }

    public async Task CreateNotificationAsync(Guid userId, string title, string message, NotificationType type)
    {
        var notification = new Notification.Domain.Entities.Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
    }
}
