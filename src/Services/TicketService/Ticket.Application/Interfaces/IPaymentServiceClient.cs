using Ticket.Application.DTOs;

namespace Ticket.Application.Interfaces;

/// <summary>
/// Called by TicketService after a vehicle exits to trigger walk-in payment.
/// </summary>
public interface IPaymentServiceClient
{
    /// <summary>
    /// Creates a payment record in PaymentService for a completed ticket.
    /// For Cash mode: payment is immediately Success.
    /// For Card/UPI/Wallet: returns a RazorpayOrderId for the frontend to open checkout.
    /// </summary>
    Task<PaymentInitResponse> CreatePaymentAsync(InitiateTicketPaymentRequest request);
};

