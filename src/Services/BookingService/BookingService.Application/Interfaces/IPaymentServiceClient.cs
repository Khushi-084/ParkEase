using BookingService.Application.DTOs;

namespace BookingService.Application.Interfaces;

public interface IPaymentServiceClient
{
    /// <summary>
    /// Calls Payment Service to create a Razorpay order.
    /// Returns the order details so the frontend can open the checkout widget.
    /// </summary>
    Task<RazorpayOrderResponse> CreateOrderAsync(CreateRazorpayOrderRequest request);
}