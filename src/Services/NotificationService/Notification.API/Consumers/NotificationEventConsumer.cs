using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.API.Hubs;
using Notification.Application.Events;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification.API.Consumers;

public class NotificationEventConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationEventConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _connection;
    private IChannel? _channel;

    public NotificationEventConsumer(
        IConfiguration config,
        ILogger<NotificationEventConsumer> logger,
        IServiceProvider serviceProvider)
    {
        _config = config;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
            UserName = _config["RabbitMQ:Username"] ?? "guest",
            Password = _config["RabbitMQ:Password"] ?? "guest"
        };

        // Retry logic for RabbitMQ connection
        int retryCount = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                break;
            }
            catch (Exception)
            {
                retryCount++;
                if (retryCount > 10) throw;
                _logger.LogWarning("[RabbitMQ] Connection failed, retrying in 5s... ({RetryCount}/10)", retryCount);
                await Task.Delay(5000, stoppingToken);
            }
        }

        await BindEventAsync("booking.events", "booking.confirmed", "notification.booking.confirmed", HandleBookingConfirmedAsync, stoppingToken);
        await BindEventAsync("auth.events", "auth.lotmanager.approved", "notification.manager.approved", HandleManagerApprovedAsync, stoppingToken);

        _logger.LogInformation("[RabbitMQ] Notification consumers started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task BindEventAsync(string exchange, string routingKey, string queueName, Func<string, Task> handler, CancellationToken ct)
    {
        await _channel!.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await _channel!.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await _channel!.QueueBindAsync(queueName, exchange, routingKey, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            try
            {
                await handler(message);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {RoutingKey}", routingKey);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
            }
        };

        await _channel.BasicConsumeAsync(queueName, false, consumer, cancellationToken: ct);
    }

    private async Task HandleBookingConfirmedAsync(string message)
    {
        var evt = JsonSerializer.Deserialize<BookingConfirmedEvent>(message);
        if (evt == null) return;

        var title = "Booking Confirmed - ParkEase";
        var content = $"Your parking slot is booked successfully. Booking ID: {evt.BookingId} at {evt.LotName}";

        await ProcessNotificationAsync(evt.UserId, evt.Email, title, content, NotificationType.Driver);
    }

    private async Task HandleManagerApprovedAsync(string message)
    {
        var evt = JsonSerializer.Deserialize<LotManagerApprovedEvent>(message);
        if (evt == null) return;

        var title = "Account Approved - ParkEase";
        var content = $"Hello {evt.ManagerName}, your parking lot has been approved and is now active.";

        await ProcessNotificationAsync(evt.UserId, evt.Email, title, content, NotificationType.Manager);
    }

    private async Task ProcessNotificationAsync(Guid userId, string email, string title, string content, NotificationType type)
    {
        _logger.LogInformation("[Notification] Processing for User: {UserId}, Email: '{Email}'", userId, email);
        
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

        // 1. Send Email
        await emailService.SendEmailAsync(email, title, content);

        // 2. Save in DB
        await notificationService.CreateNotificationAsync(userId, title, content, type);

        // 3. Send SignalR real-time notification
        await hubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", new
        {
            Title = title,
            Message = content,
            CreatedAt = DateTime.UtcNow
        });
    }

    public override async void Dispose()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        base.Dispose();
    }
}
