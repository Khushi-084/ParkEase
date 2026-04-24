using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace Payment.Application.DTOs;


public record UpdatePaymentStatusRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression(@"^(Pending|Success|Failed|Refunded)$",
        ErrorMessage = "Status must be Pending, Success, Failed, or Refunded.")]
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}
