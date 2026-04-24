using Microsoft.EntityFrameworkCore;
using Ticket.Application.Interfaces;
using Ticket.Domain.Entities;
using Ticket.Domain.Enums;
using Ticket.Infrastructure.Persistence;

namespace Ticket.Infrastructure.Repositories;

public class TicketRepository(TicketDbContext db) : ITicketRepository
{
    public async Task<TicketEntity> AddAsync(TicketEntity ticket)
    {
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return ticket;
    }

    public async Task<TicketEntity?> GetByIdAsync(Guid id) =>
        await db.Tickets.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<TicketEntity> UpdateAsync(TicketEntity ticket)
    {
        db.Tickets.Update(ticket);
        await db.SaveChangesAsync();
        return ticket;
    }

    /// <summary>
    /// FIXED: Counts Active tickets for the provided slot IDs in a single
    /// DB query. Used by ParkingLotService delete guard to detect parked vehicles.
    /// </summary>
    public async Task<int> GetActiveCountBySlotIdsAsync(IEnumerable<Guid> slotIds)
    {
        var idList = slotIds.ToList();
        if (!idList.Any()) return 0;

        return await db.Tickets
            .Where(t => t.Status == TicketStatus.Active && idList.Contains(t.SlotId))
            .CountAsync();
    }
}