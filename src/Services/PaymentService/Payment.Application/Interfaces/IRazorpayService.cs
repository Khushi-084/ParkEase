using Payment.Application.DTOs;

namespace Payment.Application.Interfaces;

/// <summary>
/// Handles all Razorpay interactions: order creation, signature verification,
/// webhook processing, and event publishing via RabbitMQ.
/// Only this service communicates with Razorpay APIs.
/// </summary>
public interface IRazorpayService
{
    /// <summary>Create a Razorpay order for the given booking.</summary>
    Task<RazorpayOrderResponse> CreateOrderAsync(CreateRazorpayOrderRequest request);

    /// <summary>
    /// Process a Razorpay webhook event (payment.captured / payment.failed).
    /// Verifies signature, ensures idempotency, and publishes to RabbitMQ.
    /// </summary>
    Task ProcessWebhookAsync(string rawBody, string razorpaySignature);

    /// <summary>
    /// Verify a client-side payment signature (called after Razorpay checkout completes).
    /// </summary>
    bool VerifySignature(string orderId, string paymentId, string signature);

    /// <summary>
    /// Verifies the signature from the client and immediately triggers PaymentSucceeded.
    /// This is used as a fallback or alternative to webhooks for frontend client verification.
    /// </summary>
    Task<bool> VerifyAndConfirmOrderAsync(VerifyRazorpayPaymentRequest request);
}