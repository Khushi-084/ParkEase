namespace Notification.Application.DTOs;

public record SmsRequestDto(
    string MobileNumber,
    string Message,
    string NotificationType
);

public record SpringEdgeRequest(
    string to,
    string message,
    string sender_id = "SPREDG",
    string type = "transactional"
);
