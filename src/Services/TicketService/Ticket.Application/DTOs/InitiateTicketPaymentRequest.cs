namespace Ticket.Application.DTOs;

/// <summary>Request sent from TicketService to PaymentService after exit.</summary>
public record InitiateTicketPaymentRequest(
    Guid    TicketId,
    decimal Amount,
    string  Mode    // Card, UPI, Wallet, Cash
);