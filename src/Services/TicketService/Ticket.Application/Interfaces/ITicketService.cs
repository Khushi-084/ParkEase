using Ticket.Application.DTOs;

namespace Ticket.Application.Interfaces;

public interface ITicketService
{
    Task<TicketResponse>     CreateTicketAsync(CreateTicketRequest request);

    /// <summary>
    /// Processes vehicle exit:
    ///   1. Calculates amount (ceiling hours × ₹20)
    ///   2. Marks slot Available
    ///   3. Sets ticket Status = Completed
    ///   4. Calls PaymentService to create a payment record
    ///
    /// Returns exit details including the payment info so the
    /// frontend knows whether to open Razorpay checkout (online modes)
    /// or just show a success screen (Cash).
    /// </summary>
    Task<ExitTicketResponse> ExitTicketAsync(string ticketIdOrDisplayId, string paymentMode);

    Task<TicketResponse>     GetByIdAsync(Guid ticketId);
    Task<int>                GetActiveCountByLotAsync(Guid lotId);
}