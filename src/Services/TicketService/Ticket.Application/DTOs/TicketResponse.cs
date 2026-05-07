using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace Ticket.Application.DTOs;

public record TicketResponse(
    Guid      Id,
    string    DisplayId,
    string    VehicleNumber,
    Guid      SlotId,
    string    SlotNumber,
    DateTime  EntryTime,
    DateTime? ExitTime,
    string    Status,
    decimal   Amount
);