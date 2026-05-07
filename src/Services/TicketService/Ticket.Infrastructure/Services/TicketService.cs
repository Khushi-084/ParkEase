using System.Text.RegularExpressions;
using Ticket.Application.DTOs;
using Ticket.Application.Interfaces;
using Ticket.Domain.Entities;
using Ticket.Domain.Enums;

namespace Ticket.Infrastructure.Services;

public partial class TicketService(
    ITicketRepository      ticketRepository,
    ISlotServiceClient     slotClient,
    IPaymentServiceClient  paymentClient) : ITicketService
{


    [GeneratedRegex(@"^[A-Z0-9 -]{4,15}$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex VehicleNumberRegex();

    // ── Entry Flow ────────────────────────────────────────────────────────────

    public async Task<TicketResponse> CreateTicketAsync(CreateTicketRequest request)
    {
        var vehicleNumber = request.VehicleNumber.Trim().ToUpperInvariant();

        if (!VehicleNumberRegex().IsMatch(vehicleNumber))
            throw new ArgumentException(
                $"VehicleNumber '{vehicleNumber}' does not match the required format (e.g. MH12AB1234).");

        var slot = await slotClient.GetAvailableSlotAsync(request.SlotType);
        await slotClient.MarkSlotOccupiedAsync(slot.SlotId);

        var ticket = new TicketEntity
        {
            DisplayId     = $"PK-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}",
            VehicleNumber = vehicleNumber,
            SlotId        = slot.SlotId,
            SlotNumber    = slot.SlotNumber,
            EntryTime     = DateTime.UtcNow,
            Status        = TicketStatus.Active,
            Amount        = 0
        };

        var saved = await ticketRepository.AddAsync(ticket);
        return MapToTicketResponse(saved);
    }

    // ── Exit Flow ─────────────────────────────────────────────────────────────

    public async Task<ExitTicketResponse> ExitTicketAsync(string ticketIdOrDisplayId, string paymentMode)
    {
        TicketEntity? ticket = null;
        if (Guid.TryParse(ticketIdOrDisplayId, out var ticketGuid))
        {
            ticket = await ticketRepository.GetByIdAsync(ticketGuid);
        }

        if (ticket == null)
        {
            ticket = await ticketRepository.GetByDisplayIdAsync(ticketIdOrDisplayId.ToUpperInvariant());
        }

        if (ticket == null)
            throw new KeyNotFoundException($"Ticket '{ticketIdOrDisplayId}' not found.");

        if (ticket.Status == TicketStatus.Completed)
            throw new InvalidOperationException(
                $"Ticket '{ticketIdOrDisplayId}' has already been completed. Double-exit is not allowed.");

        // Step 1: Calculate billing
        var exitTime    = DateTime.UtcNow;
        var rawHours    = (exitTime - ticket.EntryTime).TotalHours;
        var billedHours = Math.Ceiling(rawHours);
        // Fetch the slot to get the correct dynamic pricing
        var slot = await slotClient.GetSlotByIdAsync(ticket.SlotId);
        var amount      = (decimal)billedHours * slot.PricePerHour;

        // Step 2: Release slot
        await slotClient.MarkSlotAvailableAsync(ticket.SlotId);

        // Step 3: Complete the ticket
        ticket.ExitTime = exitTime;
        ticket.Amount   = amount;
        ticket.Status   = TicketStatus.Completed;
        var updated = await ticketRepository.UpdateAsync(ticket);

        // Step 4: Trigger payment in PaymentService
        // This is the link between TicketService and PaymentService for walk-in flow.
        var paymentResult = await paymentClient.CreatePaymentAsync(
            new InitiateTicketPaymentRequest(
                TicketId: updated.Id,
                Amount:   updated.Amount,
                Mode:     paymentMode
            ));

        return new ExitTicketResponse(
            Id:              updated.Id,
            DisplayId:       updated.DisplayId,
            VehicleNumber:   updated.VehicleNumber,
            SlotId:          updated.SlotId,
            SlotNumber:      updated.SlotNumber,
            EntryTime:       updated.EntryTime,
            ExitTime:        updated.ExitTime!.Value,
            DurationHours:   billedHours,
            Status:          updated.Status.ToString(),
            Amount:          updated.Amount,
            PaymentId:       paymentResult.PaymentId,
            PaymentStatus:   paymentResult.Status,
            RazorpayOrderId: paymentResult.RazorpayOrderId
        );
    }

    // ── Get by ID ─────────────────────────────────────────────────────────────

    public async Task<TicketResponse> GetByIdAsync(Guid ticketId)
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId)
                     ?? throw new KeyNotFoundException($"Ticket '{ticketId}' not found.");
        return MapToTicketResponse(ticket);
    }

    // ── Active-count for lot-delete guard ─────────────────────────────────────

    public async Task<int> GetActiveCountByLotAsync(Guid lotId)
    {
        var slotIds = (await slotClient.GetSlotIdsByLotAsync(lotId)).ToList();
        if (!slotIds.Any()) return 0;
        return await ticketRepository.GetActiveCountBySlotIdsAsync(slotIds);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TicketResponse MapToTicketResponse(TicketEntity t) => new(
        Id:            t.Id,
        DisplayId:     t.DisplayId,
        VehicleNumber: t.VehicleNumber,
        SlotId:        t.SlotId,
        SlotNumber:    t.SlotNumber,
        EntryTime:     t.EntryTime,
        ExitTime:      t.ExitTime,
        Status:        t.Status.ToString(),
        Amount:        t.Amount
    );
}