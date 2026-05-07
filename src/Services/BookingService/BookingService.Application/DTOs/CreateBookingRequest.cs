using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace BookingService.Application.DTOs;
 
 
/// <summary>
/// Request to create an on-the-spot booking.
/// The orchestrator will reserve the slot, create a Razorpay order,
/// and return payment details so the client can complete checkout.
/// </summary>
public record CreateBookingRequest
{
    [Required]
    [JsonPropertyName("slotId")]
    public Guid SlotId { get; init; }
 
    [Required]
    [JsonPropertyName("userId")]
    public Guid UserId { get; init; }
 
    /// <summary>Amount in INR Rupees.</summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }
}