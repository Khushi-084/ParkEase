using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace Payment.Application.DTOs;
public record RefundPaymentRequest
{
    [MaxLength(250, ErrorMessage = "Reason cannot exceed 250 characters.")]
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}