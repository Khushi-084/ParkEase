using Notification.Application.DTOs;

namespace Notification.Application.Interfaces;

public interface ISmsService
{
    Task<bool> SendSmsAsync(SmsRequestDto request);
}
