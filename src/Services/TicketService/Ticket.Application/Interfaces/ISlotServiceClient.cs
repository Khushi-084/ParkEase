using Ticket.Application.DTOs;  // Add this using statement

namespace Ticket.Application.Interfaces;

public interface ISlotServiceClient
{
    Task<SlotDTO> GetAvailableSlotAsync();
    Task<SlotDTO> GetAvailableSlotAsync(string? type);
    Task<SlotDTO> GetSlotByIdAsync(Guid slotId);
    Task<List<Guid>> GetSlotIdsByLotAsync(Guid lotId);
    Task MarkSlotOccupiedAsync(Guid slotId);
    Task MarkSlotAvailableAsync(Guid slotId);
}