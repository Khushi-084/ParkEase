using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace Ticket.Application.DTOs;
 
// ── Inbound ───────────────────────────────────────────────────────────────────
 
public record CreateTicketRequest
{
    [Required(ErrorMessage = "VehicleNumber is required.")]
    [StringLength(10, MinimumLength = 6,
        ErrorMessage = "VehicleNumber must be between 6 and 10 characters.")]
    [RegularExpression(
        @"^[A-Z]{2}[0-9]{1,2}[A-Z]{1,3}[0-9]{4}$",
        ErrorMessage = "VehicleNumber must follow Indian registration format e.g. MH12AB1234.")]
    [JsonPropertyName("vehicleNumber")]
    public string VehicleNumber { get; init; } = string.Empty;
 
    /// <summary>
    /// Optional slot type so the system allocates the correct slot.
    /// Accepted values: Car, Bike, Truck, EV. Omit for any available slot.
    /// </summary>
    [RegularExpression("^(Car|Bike|Truck|EV)$",
        ErrorMessage = "SlotType must be one of: Car, Bike, Truck, EV.")]
    [JsonPropertyName("slotType")]
    public string? SlotType { get; init; }
}