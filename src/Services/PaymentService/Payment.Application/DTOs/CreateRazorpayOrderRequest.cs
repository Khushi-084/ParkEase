using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Payment.Application.DTOs;


/// <summary>
/// Request from Booking Service to create a Razorpay order.
/// </summary>
public record CreateRazorpayOrderRequest
{
    [Required]
    [JsonPropertyName("bookingId")]
    public Guid BookingId { get; init; }
 
    [Required]
    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; init; }
 
    /// <summary>Amount in INR (not paise). Will be converted to paise before calling Razorpay.</summary>
    [Required]
    [Range(1, double.MaxValue)]
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }
 
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "INR";
}