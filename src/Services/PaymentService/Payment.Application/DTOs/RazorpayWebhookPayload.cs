using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Payment.Application.DTOs;


/// <summary>
/// Webhook payload from Razorpay (simplified shape we care about).
/// </summary>
public record RazorpayWebhookPayload
{
    [JsonPropertyName("event")]
    public string Event { get; init; } = string.Empty;
 
    [JsonPropertyName("payload")]
    public RazorpayWebhookPayloadContent? Payload { get; init; }
}