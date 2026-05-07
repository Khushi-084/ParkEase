using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace Ticket.Application.DTOs;



/// <summary>
/// Request body for the exit endpoint.
/// The attendant (or the user via app) selects the payment mode at exit time.
/// </summary>
public record ExitTicketRequest
{
    [Required(ErrorMessage = "PaymentMode is required.")]
    [RegularExpression("^(Card|UPI|Wallet|Cash)$",
        ErrorMessage = "PaymentMode must be one of: Card, UPI, Wallet, Cash.")]
    [JsonPropertyName("paymentMode")]
    public string PaymentMode { get; init; } = string.Empty;
}