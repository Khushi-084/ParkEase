using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Payment.Application.DTOs;



/// <summary>
/// Returned to Booking Service after creating a Razorpay order.
/// The Booking Service forwards this to the client so the Razorpay
/// checkout widget can be opened.
/// </summary>
public record RazorpayOrderResponse(
    string OrderId,
    string Currency,
    int    Amount,    // in paise
    string Status,
    string KeyId
);