using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Infrastructure.Services;

public class PaymentService(
    IPaymentRepository paymentRepository,
    IRazorpayService   razorpayService) : IPaymentService
{
    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<PaymentResponse> CreatePaymentAsync(CreatePaymentRequest request)
    {
        // Validate: exactly one of BookingId / TicketId must be provided
        var hasBooking = request.BookingId.HasValue && request.BookingId != Guid.Empty;
        var hasTicket  = request.TicketId.HasValue  && request.TicketId  != Guid.Empty;

        if (!hasBooking && !hasTicket)
            throw new ArgumentException("Either BookingId or TicketId must be provided.");

        if (hasBooking && hasTicket)
            throw new ArgumentException("Provide either BookingId or TicketId, not both.");

        // Parse PaymentMode
        if (!Enum.TryParse<PaymentMode>(request.Mode, true, out var mode))
            throw new ArgumentException(
                $"'{request.Mode}' is not valid. Use: Card, UPI, Wallet, Cash.");

        // Prevent duplicate active payments
        if (hasBooking)
        {
            var existing = await paymentRepository.GetActiveByBookingIdAsync(request.BookingId!.Value);
            if (existing is not null)
                throw new InvalidOperationException(
                    $"A {existing.Status} payment already exists for booking '{request.BookingId}'. " +
                    "Retry only after a Failed payment.");
        }
        else
        {
            var existing = await paymentRepository.GetActiveByTicketIdAsync(request.TicketId!.Value);
            if (existing is not null)
                throw new InvalidOperationException(
                    $"A {existing.Status} payment already exists for ticket '{request.TicketId}'. " +
                    "Retry only after a Failed payment.");
        }

        var payment = new PaymentEntity
        {
            BookingId = request.BookingId,
            TicketId  = request.TicketId,
            Amount    = request.Amount,
            Mode      = mode,
            CreatedAt = DateTime.UtcNow
        };

        // ── Cash: immediate success, no gateway ──────────────────────────────
        if (mode == PaymentMode.Cash)
        {
            payment.Status        = PaymentStatus.Success;
            payment.TransactionId = $"CASH-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N[..6]}";

            var saved = await paymentRepository.AddAsync(payment);
            return MapToResponse(saved, razorpayOrderId: null);
        }

        // ── Online (Card / UPI / Wallet): create Razorpay order ──────────────
        payment.Status        = PaymentStatus.Pending;
        payment.TransactionId = null;

        // Save first to get a PaymentId — used as correlationId for the saga
        var pending = await paymentRepository.AddAsync(payment);

        // Determine the reference ID to use as BookingId in Razorpay receipt
        // For pre-booking flow: use real BookingId
        // For walk-in flow:     use TicketId as the reference (no BookingId exists)
        var razorpayBookingId = hasBooking
            ? request.BookingId!.Value
            : request.TicketId!.Value;

        try
        {
            var rzOrder = await razorpayService.CreateOrderAsync(new CreateRazorpayOrderRequest
            {
                BookingId     = razorpayBookingId,
                CorrelationId = pending.PaymentId,   // links Razorpay order → PaymentEntity
                Amount        = request.Amount,
                Currency      = "INR"
            });

            return MapToResponse(pending, rzOrder.OrderId);
        }
        catch
        {
            // Compensation: remove the orphan Pending payment so the user can retry.
            // Without this, the duplicate check would permanently block retries
            // for this bookingId/ticketId after any Razorpay failure.
            await paymentRepository.DeleteAsync(pending.PaymentId);
            throw;
        }
    }

    // ── Update Status ─────────────────────────────────────────────────────────

    public async Task<PaymentResponse> UpdatePaymentStatusAsync(
        Guid paymentId, UpdatePaymentStatusRequest request)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId)
                      ?? throw new KeyNotFoundException($"Payment '{paymentId}' not found.");

        if (!Enum.TryParse<PaymentStatus>(request.Status, true, out var newStatus))
            throw new ArgumentException(
                $"'{request.Status}' is not valid. Use: Pending, Success, Failed, Refunded.");

        if (payment.Status == PaymentStatus.Refunded)
            throw new InvalidOperationException("A refunded payment cannot change status.");

        payment.Status = newStatus;

        var updated = await paymentRepository.UpdateAsync(payment);
        return MapToResponse(updated, razorpayOrderId: null);
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    public async Task<PaymentResponse> GetByIdAsync(Guid paymentId)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId)
                      ?? throw new KeyNotFoundException($"Payment '{paymentId}' not found.");

        return MapToResponse(payment, razorpayOrderId: null);
    }

    // ── Get by BookingId ──────────────────────────────────────────────────────

    public async Task<PaymentResponse> GetByBookingIdAsync(Guid bookingId)
    {
        var payment = await paymentRepository.GetByBookingIdAsync(bookingId)
                      ?? throw new KeyNotFoundException(
                          $"No payment found for booking '{bookingId}'.");

        return MapToResponse(payment, razorpayOrderId: null);
    }

    // ── Get by TicketId ───────────────────────────────────────────────────────

    public async Task<PaymentResponse> GetByTicketIdAsync(Guid ticketId)
    {
        var payment = await paymentRepository.GetByTicketIdAsync(ticketId)
                      ?? throw new KeyNotFoundException(
                          $"No payment found for ticket '{ticketId}'.");

        return MapToResponse(payment, razorpayOrderId: null);
    }

    // ── Refund ────────────────────────────────────────────────────────────────

    public async Task<PaymentResponse> RefundAsync(Guid paymentId, RefundPaymentRequest request)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId)
                      ?? throw new KeyNotFoundException($"Payment '{paymentId}' not found.");

        if (payment.Status != PaymentStatus.Success)
            throw new InvalidOperationException(
                $"Only Success payments can be refunded. Current status: {payment.Status}");

        payment.Status     = PaymentStatus.Refunded;
        payment.RefundedAt = DateTime.UtcNow;

        var updated = await paymentRepository.UpdateAsync(payment);
        return MapToResponse(updated, razorpayOrderId: null);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static PaymentResponse MapToResponse(PaymentEntity p, string? razorpayOrderId) => new(
        PaymentId:       p.PaymentId,
        BookingId:       p.BookingId,
        TicketId:        p.TicketId,
        Amount:          p.Amount,
        Mode:            p.Mode.ToString(),
        TransactionId:   p.TransactionId,
        Status:          p.Status.ToString(),
        CreatedAt:       p.CreatedAt,
        RefundedAt:      p.RefundedAt,
        RazorpayOrderId: razorpayOrderId
    );
}