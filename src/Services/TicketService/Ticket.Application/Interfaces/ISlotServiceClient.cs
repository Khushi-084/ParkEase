using Ticket.Application.DTOs;

namespace Ticket.Application.Interfaces;

public interface ISlotServiceClient
{
    Task<SlotDto> GetAvailableSlotAsync();
    Task MarkSlotOccupiedAsync(Guid slotId);
    Task MarkSlotAvailableAsync(Guid slotId);
}
