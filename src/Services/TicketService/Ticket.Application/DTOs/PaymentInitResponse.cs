namespace Ticket.Application.DTOs;

/// <summary>
/// Response from PaymentService.
/// RazorpayOrderId is only set for online modes.
/// </summary>
public record PaymentInitResponse(
    Guid    PaymentId,
    string  Status,
    string? RazorpayOrderId
);