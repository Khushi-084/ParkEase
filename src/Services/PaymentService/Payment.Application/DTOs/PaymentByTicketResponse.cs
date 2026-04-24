using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Payment.Application.DTOs;


public record PaymentByTicketResponse(
    Guid      PaymentId,
    Guid      TicketId,
    decimal   Amount,
    string    Mode,
    string?   TransactionId,
    string    Status,
    DateTime  CreatedAt,
    DateTime? RefundedAt
);