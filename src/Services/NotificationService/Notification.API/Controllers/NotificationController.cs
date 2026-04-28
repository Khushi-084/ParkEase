using Microsoft.AspNetCore.Mvc;
using Notification.Application.Interfaces;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationController(INotificationService notificationService) : ControllerBase
{
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserNotifications(Guid userId)
    {
        return Ok(await notificationService.GetUserNotificationsAsync(userId));
    }

    [HttpGet("unread-count/{userId:guid}")]
    public async Task<IActionResult> GetUnreadCount(Guid userId)
    {
        return Ok(await notificationService.GetUnreadCountAsync(userId));
    }

    [HttpPut("mark-read/{id:guid}")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await notificationService.MarkAsReadAsync(id);
        return NoContent();
    }

    [HttpPut("user/{userId:guid}/mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead(Guid userId)
    {
        await notificationService.MarkAllAsReadAsync(userId);
        return NoContent();
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request, [FromServices] IEmailService emailService)
    {
        await emailService.SendEmailAsync(request.ToEmail, request.Subject, request.Body);
        return Ok("Test email sent successfully.");
    }
}

public record TestEmailRequest(string ToEmail, string Subject, string Body);
