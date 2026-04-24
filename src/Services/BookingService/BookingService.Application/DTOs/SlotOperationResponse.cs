using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace BookingService.Application.DTOs;

/// <summary>Response from Slot Service reserve/confirm/release endpoints.</summary>
public record SlotOperationResponse(bool Success, string Message);