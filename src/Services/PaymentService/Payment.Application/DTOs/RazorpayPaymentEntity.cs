using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Payment.Application.DTOs;


public record RazorpayPaymentEntity
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
 
    [JsonPropertyName("order_id")]
    public string OrderId { get; init; } = string.Empty;
 
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
 
    [JsonPropertyName("notes")]
    public Dictionary<string, string>? Notes { get; init; }
}