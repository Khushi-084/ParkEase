namespace Notification.Application.DTOs;

public record EmailDto(
    string ToEmail,
    string Subject,
    string Body
);

public record NotificationResponse(
    Guid Id,
    Guid UserId,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTime CreatedAt
);
