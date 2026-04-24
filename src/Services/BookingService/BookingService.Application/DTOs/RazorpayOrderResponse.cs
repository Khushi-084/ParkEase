using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace BookingService.Application.DTOs;

/// <summary>Response from Payment Service's CreateOrder endpoint.</summary>
public record RazorpayOrderResponse(
    string OrderId,
    string Currency,
    int    Amount,      // in paise
    string Status,
    string KeyId
);
 