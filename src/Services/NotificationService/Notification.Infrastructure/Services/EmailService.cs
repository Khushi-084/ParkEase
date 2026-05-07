using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Notification.Application.Interfaces;

namespace Notification.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("[Email] Skipping send: recipient address is empty.");
            return;
        }

        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"] ?? "ParkEase", 
                _config["EmailSettings:SenderEmail"]));
            
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            var port = int.Parse(_config["EmailSettings:Port"] ?? "465");
            var options = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

            await smtp.ConnectAsync(_config["EmailSettings:SmtpServer"], port, options);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:Username"], 
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("[Email] Sent successfully to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send to {ToEmail}", toEmail);
        }
    }
}
