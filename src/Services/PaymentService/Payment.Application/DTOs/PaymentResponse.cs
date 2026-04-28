namespace Payment.Application.DTOs;

/// <summary>
/// Outbound payment response.
///
/// BookingId is set for pre-booking flow.
/// TicketId  is set for walk-in flow.
/// RazorpayOrderId is set for online payment modes (Card/UPI/Wallet).
/// </summary>
public record PaymentResponse(
    Guid      PaymentId,
    Guid?     BookingId,
    Guid?     TicketId,
    decimal   Amount,
    string    Mode,
    string?   TransactionId,
    string    Status,
    DateTime  CreatedAt,
    DateTime? RefundedAt,
    string?   RazorpayOrderId,
    string?   RazorpayKeyId
);
