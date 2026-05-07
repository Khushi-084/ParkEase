namespace Payment.Infrastructure.Messaging;

public interface IPaymentEventPublisher
{
    Task PublishPaymentSucceededAsync(Guid correlationId, string razorpayPaymentId);
    Task PublishPaymentFailedAsync(Guid correlationId, string reason);
}