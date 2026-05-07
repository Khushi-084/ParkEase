using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Payment.Application.DTOs;

/// <summary>
/// Inbound request to create a payment.
///
/// Two flows:
///   • Pre-booking  → supply BookingId, leave TicketId null
///   • Walk-in exit → supply TicketId,  leave BookingId null
///
/// Exactly one of BookingId / TicketId must be provided.
/// </summary>
public record CreatePaymentRequest
{
    /// <summary>Required for pre-booking flow. Omit for walk-in flow.</summary>
    [JsonPropertyName("bookingId")]
    public Guid? BookingId { get; init; }

    /// <summary>Required for walk-in flow. Omit for pre-booking flow.</summary>
    [JsonPropertyName("ticketId")]
    public Guid? TicketId { get; init; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [Required(ErrorMessage = "Mode is required.")]
    [RegularExpression("^(Card|UPI|Wallet|Cash)$",
        ErrorMessage = "Mode must be one of: Card, UPI, Wallet, Cash.")]
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = string.Empty;
}