using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ticket.Application.Interfaces;
using Ticket.Application.DTOs;

namespace Ticket.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client that calls PaymentService after a vehicle exits.
/// Uses Docker service name "payment-service" as the base URL (configured in appsettings).
/// </summary>
public class PaymentServiceClient(
    IHttpClientFactory httpClientFactory) : IPaymentServiceClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient Client => httpClientFactory.CreateClient("PaymentService");

    public async Task<PaymentInitResponse> CreatePaymentAsync(InitiateTicketPaymentRequest request)
    {
        // Map to the shape PaymentService's POST /api/payment expects
        var body = new
        {
            ticketId = request.TicketId,
            amount   = request.Amount,
            mode     = request.Mode
        };

        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var response = await Client.PostAsync("/api/payment", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"PaymentService returned {response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        // Deserialize the PaymentResponse from PaymentService
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        return new PaymentInitResponse(
            PaymentId:       root.GetProperty("paymentId").GetGuid(),
            Status:          root.GetProperty("status").GetString()!,
            RazorpayOrderId: root.TryGetProperty("razorpayOrderId", out var rz)
                                 ? rz.GetString()
                                 : null
        );
    }
}