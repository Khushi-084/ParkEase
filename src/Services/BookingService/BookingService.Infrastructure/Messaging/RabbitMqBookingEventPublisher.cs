using System.Text;
using System.Text.Json;
using BookingService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace BookingService.Infrastructure.Messaging;

public class RabbitMqBookingEventPublisher(
    IConfiguration config,
    ILogger<RabbitMqBookingEventPublisher> logger) : IBookingEventPublisher, IAsyncDisposable
{
    private const string ExchangeName = "booking.events";
    private IConnection? _connection;
    private IChannel?    _channel;

    private async Task EnsureConnectedAsync()
    {
        if (_channel is not null) return;

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"]     ?? "localhost",
            Port     = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        int retryCount = 0;
        while (true)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync();
                _channel    = await _connection.CreateChannelAsync();
                break;
            }
            catch (Exception)
            {
                retryCount++;
                if (retryCount > 10) throw;
                logger.LogWarning("[RabbitMQ] Booking publisher connection failed, retrying in 5s... ({RetryCount}/10)", retryCount);
                await Task.Delay(5000);
            }
        }

        await _channel!.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true);
    }

    public async Task PublishBookingConfirmedAsync(Guid userId, string email, Guid bookingId, string lotName)
    {
        await EnsureConnectedAsync();

        var payload = JsonSerializer.Serialize(new
        {
            UserId    = userId,
            Email     = email,
            BookingId = bookingId,
            LotName   = lotName
        });

        var props = new BasicProperties { Persistent = true };
        await _channel!.BasicPublishAsync(
            exchange:   ExchangeName,
            routingKey: "booking.confirmed",
            mandatory:  false,
            basicProperties: props,
            body: Encoding.UTF8.GetBytes(payload));

        logger.LogInformation("[RabbitMQ] Published BookingConfirmed for booking {BookingId}", bookingId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)    await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}
