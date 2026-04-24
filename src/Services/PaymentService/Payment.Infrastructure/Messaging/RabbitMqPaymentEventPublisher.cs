using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Payment.Infrastructure.Messaging;

/// <summary>
/// Publishes PaymentSucceeded / PaymentFailed events to RabbitMQ.
/// Booking Service subscribes and drives the saga outcome.
/// </summary>
public class RabbitMqPaymentEventPublisher(
    IConfiguration config,
    ILogger<RabbitMqPaymentEventPublisher> logger) : IPaymentEventPublisher, IAsyncDisposable
{
    private const string ExchangeName = "payment.events";

    private IConnection? _connection;
    private IChannel?    _channel;

    private async Task EnsureConnectedAsync()
    {
        if (_channel is not null) return;

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"]     ?? "rabbitmq",
            Port     = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        _connection = await factory.CreateConnectionAsync();
        _channel    = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            ExchangeName, ExchangeType.Topic, durable: true);
    }

    public async Task PublishPaymentSucceededAsync(Guid correlationId, string razorpayPaymentId)
    {
        await EnsureConnectedAsync();

        var payload = JsonSerializer.Serialize(new
        {
            CorrelationId = correlationId,
            PaymentId     = razorpayPaymentId
        });

        var props = new BasicProperties { Persistent = true };
        await _channel!.BasicPublishAsync(
            exchange:   ExchangeName,
            routingKey: "payment.succeeded",
            mandatory:  false,
            basicProperties: props,
            body: Encoding.UTF8.GetBytes(payload));

        logger.LogInformation("[RabbitMQ] Published PaymentSucceeded for correlationId {CorrelationId}",
            correlationId);
    }

    public async Task PublishPaymentFailedAsync(Guid correlationId, string reason)
    {
        await EnsureConnectedAsync();

        var payload = JsonSerializer.Serialize(new
        {
            CorrelationId = correlationId,
            Reason        = reason
        });

        var props = new BasicProperties { Persistent = true };
        await _channel!.BasicPublishAsync(
            exchange:   ExchangeName,
            routingKey: "payment.failed",
            mandatory:  false,
            basicProperties: props,
            body: Encoding.UTF8.GetBytes(payload));

        logger.LogInformation("[RabbitMQ] Published PaymentFailed for correlationId {CorrelationId}",
            correlationId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)    await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
    }
}