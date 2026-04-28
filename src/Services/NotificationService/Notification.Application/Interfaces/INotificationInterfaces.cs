using Notification.Application.DTOs;
using Notification.Domain.Entities;

namespace Notification.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public interface INotificationService
{
    Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
    Task CreateNotificationAsync(Guid userId, string title, string message, NotificationType type);
}
