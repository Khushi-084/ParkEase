using System.Text;
using System.Text.Json;
using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.Services;

/// <summary>
/// HTTP client that calls PaymentService to create Razorpay orders.
/// Uses Docker service name "payment-service" as the base URL.
/// </summary>
public class PaymentServiceClient(
    HttpClient httpClient,
    ILogger<PaymentServiceClient> logger) : IPaymentServiceClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<RazorpayOrderResponse> CreateOrderAsync(CreateRazorpayOrderRequest request)
    {
        var json    = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        logger.LogInformation("Creating Razorpay order for booking {BookingId}", request.BookingId);

        var response = await httpClient.PostAsync("/api/v1/payment/order", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("PaymentService CreateOrder failed: {Status} {Body}",
                response.StatusCode, errorBody);
            throw new InvalidOperationException(
                $"Payment service returned {response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var orderResponse = JsonSerializer.Deserialize<RazorpayOrderResponse>(responseBody, JsonOpts)
            ?? throw new InvalidOperationException("Invalid response from Payment Service.");

        return orderResponse;
    }
}
