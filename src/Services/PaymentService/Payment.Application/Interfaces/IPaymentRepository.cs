using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentEntity> AddAsync(PaymentEntity payment);
    Task<PaymentEntity?> GetByIdAsync(Guid paymentId);
    Task<PaymentEntity> UpdateAsync(PaymentEntity payment);

    // ── Pre-booking flow (BookingId) ──────────────────────────────────────────

    /// <summary>
    /// Returns any Pending or Success payment for the given booking.
    /// Failed/Refunded payments are excluded so retries are allowed.
    /// Used to prevent duplicate payments for the same booking.
    /// </summary>
    Task<PaymentEntity?> GetActiveByBookingIdAsync(Guid bookingId);

    /// <summary>
    /// Returns the most recent payment for a booking regardless of status.
    /// </summary>
    Task<PaymentEntity?> GetByBookingIdAsync(Guid bookingId);

    // ── Walk-in flow (TicketId) ───────────────────────────────────────────────

    /// <summary>
    /// Returns any Pending or Success payment for the given ticket.
    /// Failed/Refunded payments are excluded so retries are allowed.
    /// Used to prevent duplicate payments for the same ticket.
    /// </summary>
    Task<PaymentEntity?> GetActiveByTicketIdAsync(Guid ticketId);

    /// <summary>
    /// Returns the most recent payment for a ticket regardless of status.
    /// </summary>
    Task<PaymentEntity?> GetByTicketIdAsync(Guid ticketId);

    // IPaymentRepository
    Task DeleteAsync(Guid paymentId);

    
}