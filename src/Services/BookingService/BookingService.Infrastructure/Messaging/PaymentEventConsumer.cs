using System.Text;
using System.Text.Json;
using BookingService.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BookingService.Infrastructure.Messaging;

/// <summary>
/// Background service that listens to RabbitMQ for PaymentSucceeded and
/// PaymentFailed events published by PaymentService, then drives the saga.
/// </summary>
public class PaymentEventConsumer(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentEventConsumer> logger) : BackgroundService
{
    private const string ExchangeName       = "payment.events";
    private const string SuccessQueue       = "booking.payment.succeeded";
    private const string FailedQueue        = "booking.payment.failed";
    private const string SuccessRoutingKey  = "payment.succeeded";
    private const string FailedRoutingKey   = "payment.failed";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Retry loop to handle RabbitMQ not being ready at startup
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "RabbitMQ consumer error — retrying in 5s");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task RunConsumerAsync(CancellationToken stoppingToken)
    {
        var factory = BuildFactory();
        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel    = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declare exchange (topic)
        await channel.ExchangeDeclareAsync(
            ExchangeName, ExchangeType.Topic, durable: true,
            cancellationToken: stoppingToken);

        // Success queue
        await channel.QueueDeclareAsync(
            SuccessQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            SuccessQueue, ExchangeName, SuccessRoutingKey,
            cancellationToken: stoppingToken);

        // Failed queue
        await channel.QueueDeclareAsync(
            FailedQueue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            FailedQueue, ExchangeName, FailedRoutingKey,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(0, 1, false, stoppingToken);

        logger.LogInformation("PaymentEventConsumer started — listening on {Exchange}", ExchangeName);

        var successConsumer = new AsyncEventingBasicConsumer(channel);
        successConsumer.ReceivedAsync += async (_, ea) =>
            await HandleAsync(ea, channel, isSuccess: true, stoppingToken);

        var failedConsumer = new AsyncEventingBasicConsumer(channel);
        failedConsumer.ReceivedAsync += async (_, ea) =>
            await HandleAsync(ea, channel, isSuccess: false, stoppingToken);

        await channel.BasicConsumeAsync(SuccessQueue, autoAck: false, successConsumer, stoppingToken);
        await channel.BasicConsumeAsync(FailedQueue,  autoAck: false, failedConsumer,  stoppingToken);

        // Block until cancelled
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleAsync(
        BasicDeliverEventArgs ea,
        IChannel channel,
        bool isSuccess,
        CancellationToken ct)
    {
        var body    = Encoding.UTF8.GetString(ea.Body.Span);
        var payload = JsonSerializer.Deserialize<PaymentEventPayload>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (payload is null)
        {
            logger.LogWarning("Invalid payment event payload — nacking");
            await channel.BasicNackAsync(ea.DeliveryTag, false, false, ct);
            return;
        }

        logger.LogInformation("Received {EventType} for correlationId {CorrelationId}",
            isSuccess ? "PaymentSucceeded" : "PaymentFailed", payload.CorrelationId);

        try
        {
            using var scope   = scopeFactory.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            if (isSuccess)
                await bookingService.ConfirmBookingAsync(payload.CorrelationId, payload.PaymentId ?? "");
            else
                await bookingService.FailBookingAsync(payload.CorrelationId, payload.Reason ?? "unknown");

            await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling payment event for {CorrelationId}", payload.CorrelationId);
            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, ct);
        }
    }

    private ConnectionFactory BuildFactory()
    {
        using var scope = scopeFactory.CreateScope();
        var config = scope.ServiceProvider
            .GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        return new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"]     ?? "rabbitmq",
            Port     = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };
    }

    private record PaymentEventPayload(
        Guid    CorrelationId,
        string? PaymentId,
        string? Reason
    );
}