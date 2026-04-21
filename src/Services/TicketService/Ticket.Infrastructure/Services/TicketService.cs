using System.Text.RegularExpressions;
using Ticket.Application.DTOs;
using Ticket.Application.Interfaces;
using Ticket.Domain.Entities;
using Ticket.Domain.Enums;

namespace Ticket.Infrastructure.Services;

/// <summary>
/// Implements the core parking ticket lifecycle:
/// vehicle entry (slot allocation + ticket creation) and
/// vehicle exit (billing + slot release + ticket completion).
/// </summary>
public partial class TicketService(
    ITicketRepository  ticketRepository,
    ISlotServiceClient slotClient) : ITicketService
{
    /// <summary>
    /// Parking rate in INR per hour. Duration is always rounded up (ceiling).
    /// </summary>
    private const decimal RatePerHour = 20m;

    /// <summary>
    /// Compiled regex used to validate vehicle numbers before persistence.
    /// Indian format: 2 letters + 1-2 digits + 1-3 letters + 4 digits. e.g. MH12AB1234
    /// </summary>
    [GeneratedRegex(@"^[A-Z]{2}[0-9]{1,2}[A-Z]{1,3}[0-9]{4}$", RegexOptions.Compiled)]
    private static partial Regex VehicleNumberRegex();

    // ── Entry Flow ────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles vehicle entry:
    /// 1. Validates and normalises vehicle number via regex.
    /// 2. Fetches the first available slot from SlotService.
    /// 3. Marks the slot as Occupied.
    /// 4. Persists and returns the new Active ticket.
    /// </summary>
    public async Task<TicketResponse> CreateTicketAsync(CreateTicketRequest request)
    {
        // Normalise and validate vehicle number
        var vehicleNumber = request.VehicleNumber.Trim().ToUpperInvariant();

        if (!VehicleNumberRegex().IsMatch(vehicleNumber))
            throw new ArgumentException(
                $"VehicleNumber '{vehicleNumber}' does not match the required format (e.g. MH12AB1234).");

        // 1. Fetch first available slot from SlotService
        var slot = await slotClient.GetAvailableSlotAsync();

        // 2. Mark slot as Occupied
        await slotClient.MarkSlotOccupiedAsync(slot.SlotId);

        // 3. Create and persist the ticket
        var ticket = new TicketEntity
        {
            VehicleNumber = vehicleNumber,
            SlotId        = slot.SlotId,
            EntryTime     = DateTime.UtcNow,
            Status        = TicketStatus.Active,
            Amount        = 0
        };

        var saved = await ticketRepository.AddAsync(ticket);
        return MapToTicketResponse(saved);
    }

    // ── Exit Flow ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles vehicle exit:
    /// 1. Fetches the ticket and guards against double-exit.
    /// 2. Calculates parking duration (ceiling to next whole hour).
    /// 3. Applies pricing rule: Rs.20 per hour.
    /// 4. Frees the slot in SlotService.
    /// 5. Persists and returns the Completed ticket with bill details.
    /// </summary>
    public async Task<ExitTicketResponse> ExitTicketAsync(Guid ticketId)
    {
        // 1. Fetch ticket — throws 404-mappable exception if missing
        var ticket = await ticketRepository.GetByIdAsync(ticketId)
                     ?? throw new KeyNotFoundException($"Ticket '{ticketId}' not found.");

        // Guard: prevent processing an already-completed ticket
        if (ticket.Status == TicketStatus.Completed)
            throw new InvalidOperationException(
                $"Ticket '{ticketId}' has already been completed. Double-exit is not allowed.");

        // 2. Calculate duration rounded up to the nearest whole hour
        var exitTime      = DateTime.UtcNow;
        var rawHours      = (exitTime - ticket.EntryTime).TotalHours;
        var billedHours   = Math.Ceiling(rawHours);   // e.g. 1.1h → 2h

        // 3. Apply pricing: Rs.20 per billed hour
        var amount = (decimal)billedHours * RatePerHour;

        // 4. Release the slot back to Available in SlotService
        await slotClient.MarkSlotAvailableAsync(ticket.SlotId);

        // 5. Update and persist the ticket
        ticket.ExitTime = exitTime;
        ticket.Amount   = amount;
        ticket.Status   = TicketStatus.Completed;

        var updated = await ticketRepository.UpdateAsync(ticket);

        return new ExitTicketResponse(
            Id:            updated.Id,
            VehicleNumber: updated.VehicleNumber,
            SlotId:        updated.SlotId,
            EntryTime:     updated.EntryTime,
            ExitTime:      updated.ExitTime!.Value,
            DurationHours: billedHours,
            Status:        updated.Status.ToString(),
            Amount:        updated.Amount
        );
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TicketResponse MapToTicketResponse(TicketEntity t) => new(
        Id:            t.Id,
        VehicleNumber: t.VehicleNumber,
        SlotId:        t.SlotId,
        EntryTime:     t.EntryTime,
        ExitTime:      t.ExitTime,
        Status:        t.Status.ToString(),
        Amount:        t.Amount
    );
}