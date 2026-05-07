using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ticket.Domain.Enums;

namespace Ticket.Domain.Entities;

/// <summary>
/// Represents a parking ticket issued when a vehicle enters the lot.
/// Tracks the full lifecycle: entry, slot assignment, exit, and billing.
/// </summary>
[Table("Tickets")]
public class TicketEntity
{
    /// <summary>
    /// Primary key. Auto-generated GUID.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User-friendly short ID for the ticket (e.g. PK-7A2B).
    /// </summary>
    [Column("DisplayId")]
    public string DisplayId { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle registration number in Indian format e.g. MH12AB1234.
    /// Always stored in UPPERCASE.
    /// </summary>
    [Required(ErrorMessage = "VehicleNumber is required.")]
    [MaxLength(10, ErrorMessage = "VehicleNumber cannot exceed 10 characters.")]
    [MinLength(6,  ErrorMessage = "VehicleNumber must be at least 6 characters.")]
    [RegularExpression(
        @"^[A-Z]{2}[0-9]{1,2}[A-Z]{1,3}[0-9]{4}$",
        ErrorMessage = "VehicleNumber must follow Indian registration format e.g. MH12AB1234.")]
    [Column("VehicleNumber", TypeName = "character varying(10)")]
    public string VehicleNumber { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key reference to the slot assigned to this vehicle.
    /// Links to the SlotService domain (cross-service — no navigation property).
    /// </summary>
    [Required(ErrorMessage = "SlotId is required.")]
    [Column("SlotId")]
    public Guid SlotId { get; set; }

    /// <summary>
    /// User-friendly slot number (e.g. A-01). Cached for display.
    /// </summary>
    [Column("SlotNumber")]
    public string SlotNumber { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp at which the vehicle entered the parking lot.
    /// </summary>
    [Required]
    [Column("EntryTime")]
    public DateTime EntryTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// UTC timestamp at which the vehicle exited.
    /// Null while the ticket is still Active.
    /// </summary>
    [Column("ExitTime")]
    public DateTime? ExitTime { get; set; }

    /// <summary>
    /// Lifecycle status of the ticket.
    /// Active  → vehicle is currently parked.
    /// Completed → vehicle has exited and bill is settled.
    /// </summary>
    [Required]
    [EnumDataType(typeof(TicketStatus), ErrorMessage = "Status must be Active or Completed.")]
    [Column("Status", TypeName = "text")]
    public TicketStatus Status { get; set; } = TicketStatus.Active;

    /// <summary>
    /// Total parking charge in INR.
    /// Calculated as Rs.20 x ceiling(parking hours).
    /// Zero until the vehicle exits.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Amount cannot be negative.")]
    [Column("Amount", TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; } = 0;
}