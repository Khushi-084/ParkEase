using Slot.Application.DTOs;
using Slot.Application.Interfaces;
using Slot.Domain.Entities;
using Slot.Domain.Enums;

namespace Slot.Infrastructure.Services;

public class SlotService(ISlotRepository repo) : ISlotService
{
    private const string ValidTypes = "Car, Bike, Truck, EV";

    // ── Existing methods (unchanged) ──────────────────────────────────────────

    public async Task<SlotResponse> CreateAsync(CreateSlotRequest req)
    {
        if (!Enum.TryParse<SlotType>(req.Type, ignoreCase: true, out var slotType))
            throw new ArgumentException($"Invalid slot type '{req.Type}'. Use: {ValidTypes}.");

        if (await repo.ExistsBySlotNumberAsync(req.LotId, req.SlotNumber))
            throw new InvalidOperationException($"Slot '{req.SlotNumber}' already exists in this lot.");

        var slot = new SlotEntity
        {
            LotId        = req.LotId,
            SlotNumber   = req.SlotNumber.Trim().ToUpper(),
            Type         = slotType,
            PricePerHour = req.PricePerHour
        };

        await repo.AddAsync(slot);
        await repo.SaveChangesAsync();
        return Map(slot);
    }

    public async Task<IEnumerable<SlotResponse>> BulkCreateAsync(BulkCreateSlotRequest req)
    {
        if (!Enum.TryParse<SlotType>(req.Type, ignoreCase: true, out var slotType))
            throw new ArgumentException($"Invalid slot type '{req.Type}'. Use: {ValidTypes}.");

        var existingNumbers = (await repo.GetExistingSlotNumbersAsync(req.LotId))
                              .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var slots = new List<SlotEntity>();

        for (int i = 1; i <= req.Count; i++)
        {
            var slotNumber = $"{req.Prefix.ToUpper()}-{i:D2}";
            if (existingNumbers.Contains(slotNumber))
                throw new InvalidOperationException($"Slot '{slotNumber}' already exists in this lot.");

            slots.Add(new SlotEntity
            {
                LotId        = req.LotId,
                SlotNumber   = slotNumber,
                Type         = slotType,
                PricePerHour = req.PricePerHour
            });
        }

        await repo.AddRangeAsync(slots);
        await repo.SaveChangesAsync();
        return slots.Select(Map);
    }

    public async Task<SlotResponse> GetByIdAsync(Guid slotId)
    {
        var slot = await repo.GetByIdAsync(slotId)
            ?? throw new KeyNotFoundException($"Slot '{slotId}' not found.");
        return Map(slot);
    }

    public async Task<IEnumerable<SlotResponse>> GetByLotIdAsync(Guid lotId)
        => (await repo.GetByLotIdAsync(lotId)).Select(Map);

    public async Task<SlotAvailabilityResponse> GetAvailabilityAsync(Guid lotId, string? type = null)
    {
        SlotType? slotType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<SlotType>(type, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Invalid slot type '{type}'. Use: {ValidTypes}.");
            slotType = parsed;
        }

        var allSlots       = (await repo.GetByLotIdAsync(lotId)).ToList();
        var availableSlots = (await repo.GetAvailableByLotIdAsync(lotId, slotType)).ToList();

        return new SlotAvailabilityResponse(
            lotId,
            allSlots.Count,
            allSlots.Count(s => s.Status == SlotStatus.Available),
            allSlots.Count(s => s.Status == SlotStatus.Occupied),
            allSlots.Count(s => s.Status == SlotStatus.Reserved),
            availableSlots.Select(Map)
        );
    }

    public async Task<SlotResponse> UpdateAsync(Guid slotId, UpdateSlotRequest req)
    {
        var slot = await repo.GetByIdAsync(slotId)
            ?? throw new KeyNotFoundException($"Slot '{slotId}' not found.");

        if (!Enum.TryParse<SlotType>(req.Type, ignoreCase: true, out var slotType))
            throw new ArgumentException($"Invalid slot type '{req.Type}'. Use: {ValidTypes}.");

        if (await repo.ExistsBySlotNumberAsync(slot.LotId, req.SlotNumber, excludeId: slotId))
            throw new InvalidOperationException($"Slot '{req.SlotNumber}' already exists in this lot.");

        slot.SlotNumber   = req.SlotNumber.Trim().ToUpper();
        slot.Type         = slotType;
        slot.PricePerHour = req.PricePerHour;
        slot.UpdatedAt    = DateTime.UtcNow;

        await repo.SaveChangesAsync();
        return Map(slot);
    }

    public async Task<SlotResponse> UpdateStatusAsync(Guid slotId, SlotStatusUpdateRequest req)
    {
        var slot = await repo.GetByIdAsync(slotId)
            ?? throw new KeyNotFoundException($"Slot '{slotId}' not found.");

        if (!Enum.TryParse<SlotStatus>(req.Status, ignoreCase: true, out var newStatus))
            throw new ArgumentException($"Invalid status '{req.Status}'.");

        slot.Status    = newStatus;
        slot.UpdatedAt = DateTime.UtcNow;

        await repo.SaveChangesAsync();
        return Map(slot);
    }

    public async Task<SlotResponse> GetFirstAvailableAsync(string? type = null)
    {
        SlotType? slotType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<SlotType>(type, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Invalid slot type '{type}'. Use: {ValidTypes}.");
            slotType = parsed;
        }

        var slot = await repo.GetFirstAvailableAsync(slotType)
            ?? throw new KeyNotFoundException(
                slotType.HasValue
                    ? $"No available {slotType} slots found."
                    : "No available slots found.");
        return Map(slot);
    }

    public async Task DeleteAsync(Guid slotId)
    {
        var slot = await repo.GetByIdAsync(slotId)
            ?? throw new KeyNotFoundException($"Slot '{slotId}' not found.");
        await repo.DeleteAsync(slot);
    }

    // ── NEW: Booking Saga operations ──────────────────────────────────────────

    /// <summary>
    /// Reserve a slot for a pending booking (Available → Reserved).
    /// Part of the Saga: called synchronously by Booking Service before
    /// creating a Razorpay order.
    /// </summary>
    public async Task<SlotResponse> ReserveSlotAsync(Guid slotId)
    {
        var slot = await repo.GetByIdAsync(slotId)
            ?? throw new KeyNotFoundException($"Slot '{slotId}' not found.");

        if (slot.Status != SlotStatus.Available)
            throw new InvalidOperationException(
                $"Slot '{slotId}' cannot be reserved — current status is '{slot.Status}'. " +
                "Only Available slots can be reserved.");

        slot.Status    = SlotStatus.Reserved;
        slot.UpdatedAt = DateTime.UtcNow;

        await repo.SaveChangesAsync();
        return Map(slot);
    }

    /// <summary>
    /// Confirm a reserved slot after payment succeeds (Reserved → Occupied).
    /// Saga compensation step on PaymentSucceeded event.
    /// </summary>
    public async Task<SlotResponse> ConfirmSlotAsync(Guid slotId)
    {
        var slot = await repo.GetByIdAsync(slotId)
            ?? throw new KeyNotFoundException($"Slot '{slotId}' not found.");

        if (slot.Status != SlotStatus.Reserved)
            throw new InvalidOperationException(
                $"Slot '{slotId}' cannot be confirmed — current status is '{slot.Status}'. " +
                "Only Reserved slots can be confirmed.");

        slot.Status    = SlotStatus.Occupied;
        slot.UpdatedAt = DateTime.UtcNow;

        await repo.SaveChangesAsync();
        return Map(slot);
    }

    /// <summary>
    /// Release a reserved slot after payment fails (Reserved → Available).
    /// Saga compensation step on PaymentFailed event.
    /// </summary>
    public async Task<SlotResponse> ReleaseSlotAsync(Guid slotId)
    {
        var slot = await repo.GetByIdAsync(slotId)
            ?? throw new KeyNotFoundException($"Slot '{slotId}' not found.");

        // Idempotent: if already Available, nothing to do
        if (slot.Status == SlotStatus.Available)
            return Map(slot);

        if (slot.Status != SlotStatus.Reserved)
            throw new InvalidOperationException(
                $"Slot '{slotId}' cannot be released — current status is '{slot.Status}'. " +
                "Only Reserved slots can be released back to Available.");

        slot.Status    = SlotStatus.Available;
        slot.UpdatedAt = DateTime.UtcNow;

        await repo.SaveChangesAsync();
        return Map(slot);
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static SlotResponse Map(SlotEntity s) => new(
        s.SlotId, s.LotId, s.SlotNumber,
        s.Type.ToString(), s.Status.ToString(),
        s.PricePerHour, s.CreatedAt, s.UpdatedAt);
}