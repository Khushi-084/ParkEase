namespace Notification.Domain.Entities;

public class NotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MobileNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Sent, Failed
    public string Provider { get; set; } = "SpringEdge";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
