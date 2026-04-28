using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
 
namespace Ticket.Application.DTOs;

/// <summary>
/// Returned after a vehicle exits.
/// Includes payment info so the frontend knows what to do next:
///   • Cash           → show "Payment collected" screen
///   • Card/UPI/Wallet → open Razorpay checkout using RazorpayOrderId
/// </summary>
public record ExitTicketResponse(
    Guid     Id,
    string   DisplayId,
    string   VehicleNumber,
    Guid     SlotId,
    string   SlotNumber,
    DateTime EntryTime,
    DateTime ExitTime,
    double   DurationHours,
    string   Status,
    decimal  Amount,
    // Payment info
    Guid     PaymentId,
    string   PaymentStatus,
    string?  RazorpayOrderId    // null for Cash, set for online modes
);