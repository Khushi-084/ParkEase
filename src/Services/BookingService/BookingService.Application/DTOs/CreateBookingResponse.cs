using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace BookingService.Application.DTOs;

/// <summary>
/// Returned after initiating a booking. Contains Razorpay order details
/// the client needs to open the checkout widget.
/// </summary>
public record CreateBookingResponse(
    Guid    BookingId,
    string  DisplayId,
    Guid    SlotId,
    Guid    UserId,
    decimal Amount,
    string  Status,
    string  RazorpayOrderId,
    string  RazorpayKeyId,
    string  Currency,
    Guid    CorrelationId,
    DateTime CreatedAt
);