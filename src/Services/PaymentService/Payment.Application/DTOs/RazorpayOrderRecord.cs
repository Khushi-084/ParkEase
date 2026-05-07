using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Payment.Application.DTOs;



/// <summary>
/// Persisted record of a Razorpay order we created.
/// Used for idempotency: we don't process the same webhook twice.
/// </summary>
public record RazorpayOrderRecord(
    Guid   Id,
    Guid   BookingId,
    Guid   CorrelationId,
    string RazorpayOrderId,
    string Status,
    DateTime CreatedAt
);
