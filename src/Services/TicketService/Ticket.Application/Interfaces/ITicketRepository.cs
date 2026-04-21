using Ticket.Domain.Entities;

namespace Ticket.Application.Interfaces;

public interface ITicketRepository
{
    Task<TicketEntity>  AddAsync(TicketEntity ticket);
    Task<TicketEntity?> GetByIdAsync(Guid id);
    Task<TicketEntity>  UpdateAsync(TicketEntity ticket);
}