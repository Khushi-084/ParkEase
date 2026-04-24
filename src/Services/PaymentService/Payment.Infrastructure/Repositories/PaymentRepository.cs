using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Repositories;

public class PaymentRepository(PaymentDbContext db) : IPaymentRepository
{
    public async Task<PaymentEntity> AddAsync(PaymentEntity payment)
    {
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return payment;
    }

    public async Task<PaymentEntity?> GetByIdAsync(Guid paymentId) =>
        await db.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);

    public async Task<PaymentEntity> UpdateAsync(PaymentEntity payment)
    {
        db.Payments.Update(payment);
        await db.SaveChangesAsync();
        return payment;
    }

    /// <summary>
    /// Deletes a payment record. Used to remove orphan Pending payments
    /// when Razorpay order creation fails — so retries aren't blocked.
    /// </summary>
    public async Task DeleteAsync(Guid paymentId)
    {
        var payment = await db.Payments.FindAsync(paymentId);
        if (payment is not null)
        {
            db.Payments.Remove(payment);
            await db.SaveChangesAsync();
        }
    }

    // ── Pre-booking flow (BookingId) ──────────────────────────────────────────

    public async Task<PaymentEntity?> GetActiveByBookingIdAsync(Guid bookingId) =>
        await db.Payments
            .Where(p => p.BookingId == bookingId
                     && p.Status != PaymentStatus.Failed
                     && p.Status != PaymentStatus.Refunded)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<PaymentEntity?> GetByBookingIdAsync(Guid bookingId) =>
        await db.Payments
            .Where(p => p.BookingId == bookingId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

    // ── Walk-in flow (TicketId) ───────────────────────────────────────────────

    public async Task<PaymentEntity?> GetActiveByTicketIdAsync(Guid ticketId) =>
        await db.Payments
            .Where(p => p.TicketId == ticketId
                     && p.Status != PaymentStatus.Failed
                     && p.Status != PaymentStatus.Refunded)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<PaymentEntity?> GetByTicketIdAsync(Guid ticketId) =>
        await db.Payments
            .Where(p => p.TicketId == ticketId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
}