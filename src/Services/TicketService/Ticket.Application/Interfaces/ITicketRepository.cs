using Ticket.Domain.Entities;

namespace Ticket.Application.Interfaces;

public interface ITicketRepository
{
    Task<TicketEntity>  AddAsync(TicketEntity ticket);
    Task<TicketEntity?> GetByIdAsync(Guid id);
    Task<TicketEntity>  UpdateAsync(TicketEntity ticket);

    /// <summary>
    /// FIXED: Returns the count of Active (non-completed) tickets whose SlotId
    /// is in the provided list. Used by GetActiveCountByLotAsync to support
    /// the lot-delete guard in ParkingLotService.
    /// </summary>
    Task<int> GetActiveCountBySlotIdsAsync(IEnumerable<Guid> slotIds);
}