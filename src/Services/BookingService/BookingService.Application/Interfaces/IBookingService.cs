using BookingService.Application.DTOs;

namespace BookingService.Application.Interfaces;

public interface IBookingService
{
    /// <summary>Orchestrate: reserve slot → create booking → create Razorpay order.</summary>
    Task<CreateBookingResponse> CreateBookingAsync(CreateBookingRequest request);

    Task<BookingResponse>  GetByIdAsync(Guid bookingId);
    Task<IEnumerable<BookingResponse>> GetByUserIdAsync(Guid userId);

    /// <summary>Called by RabbitMQ consumer when PaymentSucceeded arrives.</summary>
    Task ConfirmBookingAsync(Guid correlationId, string razorpayPaymentId);

    /// <summary>Called by RabbitMQ consumer when PaymentFailed arrives.</summary>
    Task FailBookingAsync(Guid correlationId, string reason);
}