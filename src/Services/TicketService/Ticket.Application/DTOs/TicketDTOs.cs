using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ticket.Application.DTOs;

// ── Inbound ───────────────────────────────────────────────────────────────────

/// <summary>
/// Request payload to create a new parking ticket (vehicle entry).
/// </summary>
public record CreateTicketRequest
{
    /// <summary>
    /// Vehicle registration number.
    /// Format: 2 uppercase letters + 1-2 digits + 1-3 uppercase letters + 4 digits.
    /// Example: MH12AB1234
    /// </summary>
    [Required(ErrorMessage = "VehicleNumber is required.")]
    [StringLength(10, MinimumLength = 6, ErrorMessage = "VehicleNumber must be between 6 and 10 characters.")]
    [RegularExpression(
        @"^[A-Z]{2}[0-9]{1,2}[A-Z]{1,3}[0-9]{4}$",
        ErrorMessage = "VehicleNumber must follow Indian registration format e.g. MH12AB1234.")]
    [JsonPropertyName("vehicleNumber")]
    public string VehicleNumber { get; init; } = string.Empty;
}

// ── Outbound ──────────────────────────────────────────────────────────────────

/// <summary>
/// Returned after a successful vehicle entry (ticket creation).
/// </summary>
public record TicketResponse(
    /// <summary>Unique ticket identifier (GUID).</summary>
    Guid      Id,
    /// <summary>Normalised vehicle registration number.</summary>
    string    VehicleNumber,
    /// <summary>ID of the parking slot assigned to this vehicle.</summary>
    Guid      SlotId,
    /// <summary>UTC timestamp when the vehicle entered.</summary>
    DateTime  EntryTime,
    /// <summary>UTC timestamp when the vehicle exited. Null if still parked.</summary>
    DateTime? ExitTime,
    /// <summary>Ticket lifecycle status: Active or Completed.</summary>
    string    Status,
    /// <summary>Total parking charge in INR. Zero until exit.</summary>
    decimal   Amount
);

/// <summary>
/// Returned after a successful vehicle exit with full billing details.
/// </summary>
public record ExitTicketResponse(
    /// <summary>Unique ticket identifier (GUID).</summary>
    Guid     Id,
    /// <summary>Normalised vehicle registration number.</summary>
    string   VehicleNumber,
    /// <summary>ID of the parking slot that was released.</summary>
    Guid     SlotId,
    /// <summary>UTC timestamp when the vehicle entered.</summary>
    DateTime EntryTime,
    /// <summary>UTC timestamp when the vehicle exited.</summary>
    DateTime ExitTime,
    /// <summary>Parking duration in whole hours (rounded up to ceiling).</summary>
    double   DurationHours,
    /// <summary>Final ticket status — always Completed at this point.</summary>
    string   Status,
    /// <summary>Total parking charge in INR calculated as Rs.20 x ceiling(hours).</summary>
    decimal  Amount
);

// ── SlotService integration DTOs ─────────────────────────────────────────────

/// <summary>
/// Mirrors the slot response shape returned by the Slot microservice.
/// Kept in Application layer so TicketService has no hard reference to SlotService.
/// </summary>
public record SlotDto(
    Guid     SlotId,
    Guid     LotId,
    string   SlotNumber,
    string   Type,
    string   Status,
    decimal  PricePerHour,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Payload sent to PATCH /api/v1/slots/{id}/status in SlotService.</summary>
public record SlotStatusUpdateDto(
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression(@"^(Available|Occupied|Reserved)$",
        ErrorMessage = "Status must be Available, Occupied, or Reserved.")]
    string Status
);