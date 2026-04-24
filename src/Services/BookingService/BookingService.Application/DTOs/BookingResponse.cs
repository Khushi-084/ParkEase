using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace BookingService.Application.DTOs;



public record BookingResponse(
    Guid     Id,
    Guid     SlotId,
    Guid     UserId,
    decimal  Amount,
    string   Status,
    string?  RazorpayOrderId,
    Guid     CorrelationId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);