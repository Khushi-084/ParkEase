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
    private const decimal RatePerHour = 20m;

    [GeneratedRegex(@"^[A-Z]{2}[0-9]{1,2}[A-Z]{1,3}[0-9]{4}$", RegexOptions.Compiled)]
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

    public async Task<ExitTicketResponse> ExitTicketAsync(Guid ticketId, string paymentMode)
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId)
                     ?? throw new KeyNotFoundException($"Ticket '{ticketId}' not found.");

        if (ticket.Status == TicketStatus.Completed)
            throw new InvalidOperationException(
                $"Ticket '{ticketId}' has already been completed. Double-exit is not allowed.");

        // Step 1: Calculate billing
        var exitTime    = DateTime.UtcNow;
        var rawHours    = (exitTime - ticket.EntryTime).TotalHours;
        var billedHours = Math.Ceiling(rawHours);
        var amount      = (decimal)billedHours * RatePerHour;

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
            VehicleNumber:   updated.VehicleNumber,
            SlotId:          updated.SlotId,
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
        VehicleNumber: t.VehicleNumber,
        SlotId:        t.SlotId,
        EntryTime:     t.EntryTime,
        ExitTime:      t.ExitTime,
        Status:        t.Status.ToString(),
        Amount:        t.Amount
    );
}