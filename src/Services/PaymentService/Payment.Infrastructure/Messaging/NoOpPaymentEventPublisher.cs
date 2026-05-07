using Microsoft.Extensions.Logging;

namespace Payment.Infrastructure.Messaging;

public class NoOpPaymentEventPublisher(
    ILogger<NoOpPaymentEventPublisher> logger) : IPaymentEventPublisher
{
    public Task PublishPaymentSucceededAsync(Guid correlationId, string razorpayPaymentId)
    {
        logger.LogWarning(
            "RabbitMQ publishing is disabled. Skipping PaymentSucceeded event for correlationId {CorrelationId}",
            correlationId);
        return Task.CompletedTask;
    }

    public Task PublishPaymentFailedAsync(Guid correlationId, string reason)
    {
        logger.LogWarning(
            "RabbitMQ publishing is disabled. Skipping PaymentFailed event for correlationId {CorrelationId}",
            correlationId);
        return Task.CompletedTask;
    }
}
