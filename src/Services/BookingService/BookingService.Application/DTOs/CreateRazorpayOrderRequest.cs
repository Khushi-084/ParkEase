using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace BookingService.Application.DTOs;


/// <summary>Internal request to Payment Service.</summary>
public record CreateRazorpayOrderRequest(
    Guid    BookingId,
    Guid    CorrelationId,
    decimal Amount,
    string  Currency = "INR"
);