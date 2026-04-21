using Ticket.Application.DTOs;

namespace Ticket.Application.Interfaces;

public interface ITicketService
{
    Task<TicketResponse>     CreateTicketAsync(CreateTicketRequest request);
    Task<ExitTicketResponse> ExitTicketAsync(Guid ticketId);
}
