using Microsoft.EntityFrameworkCore;
using Ticket.Application.Interfaces;
using Ticket.Domain.Entities;
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
}
