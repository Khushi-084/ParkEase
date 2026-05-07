using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Services;

/// <summary>
/// Handles Razorpay API calls (test mode), webhook processing, and RabbitMQ publishing.
/// </summary>
public class RazorpayService(
    PaymentDbContext db,
    IConfiguration   config,
    IPaymentEventPublisher publisher,
    ILogger<RazorpayService> logger) : IRazorpayService
{
    private static readonly HttpClient _http = new();

    // ── Create Razorpay Order ─────────────────────────────────────────────────

    public async Task<RazorpayOrderResponse> CreateOrderAsync(CreateRazorpayOrderRequest request)
    {
        var keyId     = config["Razorpay:KeyId"]     ?? throw new InvalidOperationException("Razorpay:KeyId not configured");
        var keySecret = config["Razorpay:KeySecret"] ?? throw new InvalidOperationException("Razorpay:KeySecret not configured");

        int amountInPaise = (int)(request.Amount * 100);

        var orderPayload = new
        {
            amount   = amountInPaise,
            currency = request.Currency,
            receipt  = $"booking_{request.BookingId:N}",
            notes    = new
            {
                bookingId     = request.BookingId.ToString(),
                correlationId = request.CorrelationId.ToString()
            }
        };

        var json          = JsonSerializer.Serialize(orderPayload);
        var httpRequest   = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
        httpRequest.Headers.Add("Authorization", $"Basic {credentials}");

        logger.LogInformation("[Razorpay] Creating order for booking {BookingId}, amount={Amount} paise",
            request.BookingId, amountInPaise);

        var response = await _http.SendAsync(httpRequest);
        var body     = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("[Razorpay] CreateOrder failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Razorpay order creation failed: {body}");
        }

        using var doc   = JsonDocument.Parse(body);
        var root        = doc.RootElement;
        var rzOrderId   = root.GetProperty("id").GetString()!;
        var rzCurrency  = root.GetProperty("currency").GetString()!;
        var rzAmount    = root.GetProperty("amount").GetInt32();
        var rzStatus    = root.GetProperty("status").GetString()!;

        // Persist for idempotency tracking
        var orderRecord = new RazorpayOrderEntity
        {
            BookingId       = request.BookingId,
            CorrelationId   = request.CorrelationId,
            RazorpayOrderId = rzOrderId,
            AmountInPaise   = amountInPaise,
            Currency        = rzCurrency,
            Status          = rzStatus
        };
        await db.RazorpayOrders.AddAsync(orderRecord);
        await db.SaveChangesAsync();

        logger.LogInformation("[Razorpay] Order {OrderId} created for booking {BookingId}",
            rzOrderId, request.BookingId);

        return new RazorpayOrderResponse(rzOrderId, rzCurrency, rzAmount, rzStatus, keyId);
    }

    // ── Webhook ───────────────────────────────────────────────────────────────

    public async Task ProcessWebhookAsync(string rawBody, string razorpaySignature)
    {
        var secret = config["Razorpay:WebhookSecret"]
            ?? throw new InvalidOperationException("Razorpay:WebhookSecret not configured");

        // Verify HMAC-SHA256 signature
        if (!VerifyWebhookSignature(rawBody, razorpaySignature, secret))
        {
            logger.LogWarning("[Razorpay] Webhook signature verification failed");
            throw new UnauthorizedAccessException("Invalid webhook signature.");
        }

        var payload = JsonSerializer.Deserialize<RazorpayWebhookPayload>(rawBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (payload?.Payload?.Payment?.Entity is not { } entity)
        {
            logger.LogWarning("[Razorpay] Webhook payload missing payment entity");
            return;
        }

        var rzOrderId = entity.OrderId;
        logger.LogInformation("[Razorpay] Webhook event={Event}, orderId={OrderId}",
            payload.Event, rzOrderId);

        // Look up our internal order record
        var order = await db.RazorpayOrders
            .FirstOrDefaultAsync(o => o.RazorpayOrderId == rzOrderId);

        if (order is null)
        {
            logger.LogWarning("[Razorpay] No order record found for Razorpay order {OrderId}", rzOrderId);
            return;
        }

        // Idempotency check
        if (order.WebhookProcessed)
        {
            logger.LogInformation("[Razorpay] Webhook already processed for order {OrderId} — skipping",
                rzOrderId);
            return;
        }

        order.WebhookProcessed = true;
        order.UpdatedAt        = DateTime.UtcNow;

        if (payload.Event == "payment.captured")
        {
            order.Status = "paid";
            await db.SaveChangesAsync();
            await publisher.PublishPaymentSucceededAsync(order.CorrelationId, entity.Id);
            logger.LogInformation("[Razorpay] PaymentSucceeded published for correlationId {CorrelationId}",
                order.CorrelationId);
        }
        else if (payload.Event == "payment.failed")
        {
            order.Status = "failed";
            await db.SaveChangesAsync();
            await publisher.PublishPaymentFailedAsync(order.CorrelationId, "Razorpay payment.failed event");
            logger.LogInformation("[Razorpay] PaymentFailed published for correlationId {CorrelationId}",
                order.CorrelationId);
        }
        else
        {
            await db.SaveChangesAsync(); // save WebhookProcessed=true anyway
            logger.LogInformation("[Razorpay] Unhandled event type: {Event}", payload.Event);
        }
    }

    // ── Client-side signature verification ────────────────────────────────────

    public bool VerifySignature(string orderId, string paymentId, string signature)
    {
        var keySecret = config["Razorpay:KeySecret"]
            ?? throw new InvalidOperationException("Razorpay:KeySecret not configured");

        var data = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keySecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        var expected = BitConverter.ToString(hash).Replace("-", "").ToLower();
        return expected == signature;
    }

    public async Task<bool> VerifyAndConfirmOrderAsync(VerifyRazorpayPaymentRequest request)
    {
        if (!VerifySignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature))
        {
            logger.LogWarning("[Razorpay] Client-side signature verification failed for order {OrderId}", request.RazorpayOrderId);
            return false;
        }

        var order = await db.RazorpayOrders
            .FirstOrDefaultAsync(o => o.RazorpayOrderId == request.RazorpayOrderId);

        if (order is null)
        {
            logger.LogWarning("[Razorpay] No order record found for verified Razorpay order {OrderId}", request.RazorpayOrderId);
            return false;
        }

        if (order.Status == "paid" || order.WebhookProcessed)
        {
            logger.LogInformation("[Razorpay] Order {OrderId} already marked as paid", request.RazorpayOrderId);
            return true;
        }

        order.Status = "paid";
        // Do not set WebhookProcessed = true here, let the webhook do it if it arrives later.
        order.UpdatedAt = DateTime.UtcNow;

        // Also update PaymentEntity if it exists (links via CorrelationId which is the PaymentId)
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.PaymentId == order.CorrelationId);
        if (payment != null && payment.Status == PaymentStatus.Pending)
        {
            payment.Status = PaymentStatus.Success;
            payment.TransactionId = request.RazorpayPaymentId;
            logger.LogInformation("[Razorpay] Associated Payment record {PaymentId} marked as Success", payment.PaymentId);
        }

        await db.SaveChangesAsync();

        try 
        {
            await publisher.PublishPaymentSucceededAsync(order.CorrelationId, request.RazorpayPaymentId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Razorpay] Failed to publish PaymentSucceeded via RabbitMQ. Relying exclusively on HTTP fallback.");
        }

        // Fallback: direct HTTP call to BookingService to ensure status updates immediately even if RabbitMQ is down
        try
        {
            using var client = new HttpClient();
            var bookingServiceUrl = config["Services:BookingService"] ?? "http://localhost:5204";
            await client.PostAsync($"{bookingServiceUrl}/api/v1/bookings/internal/confirm/{order.CorrelationId}", null);
            logger.LogInformation("[Razorpay] Direct HTTP confirmation fallback succeeded for correlationId {CorrelationId} at {Url}", 
                order.CorrelationId, bookingServiceUrl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Razorpay] Failed to send direct confirmation fallback to BookingService.");
        }
        
        logger.LogInformation("[Razorpay] Client-side verification succeeded for correlationId {CorrelationId}", order.CorrelationId);

        return true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool VerifyWebhookSignature(string body, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash     = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expected = BitConverter.ToString(hash).Replace("-", "").ToLower();
        return expected == signature;
    }
}