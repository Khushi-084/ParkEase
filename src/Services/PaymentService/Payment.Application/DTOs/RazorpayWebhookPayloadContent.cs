using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace Payment.Application.DTOs;


public record RazorpayWebhookPayloadContent
{
    [JsonPropertyName("payment")]
    public RazorpayWebhookItem? Payment { get; init; }
}