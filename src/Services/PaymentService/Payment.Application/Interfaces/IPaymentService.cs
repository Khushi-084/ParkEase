using Payment.Application.DTOs;

namespace Payment.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> CreatePaymentAsync(CreatePaymentRequest request);
    Task<PaymentResponse> UpdatePaymentStatusAsync(Guid paymentId, UpdatePaymentStatusRequest request);
    Task<PaymentResponse> GetByIdAsync(Guid paymentId);
    Task<PaymentResponse> GetByBookingIdAsync(Guid bookingId);
    Task<PaymentResponse> GetByTicketIdAsync(Guid ticketId);
    Task<PaymentResponse> RefundAsync(Guid paymentId, RefundPaymentRequest request);
}