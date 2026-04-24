using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Payment.Application.DTOs;



public record RazorpayWebhookItem
{
    [JsonPropertyName("entity")]
    public RazorpayPaymentEntity? Entity { get; init; }
}