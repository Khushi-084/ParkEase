using Slot.Application.DTOs;
using Slot.Application.Interfaces;
using Slot.Domain.Entities;
using Slot.Domain.Enums;

namespace Slot.Infrastructure.Services;

public class SlotService(ISlotRepository repo) : ISlotService
{
    public async Task<SlotResponse> CreateAsync(CreateSlotRequest req)
    {
        if (!Enum.TryParse<SlotType>(req.Type, ignoreCase: true, out var slotType))
            throw new ArgumentException($"Invalid slot type '{req.Type}'. Use: Car, Bike, Truck.");

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
            throw new ArgumentException($"Invalid slot type '{req.Type}'. Use: Car, Bike, Truck.");

        var slots = new List<SlotEntity>();

        for (int i = 1; i <= req.Count; i++)
        {
            var slotNumber = $"{req.Prefix.ToUpper()}-{i:D2}";

            if (await repo.ExistsBySlotNumberAsync(req.LotId, slotNumber))
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
    {
        var slots = await repo.GetByLotIdAsync(lotId);
        return slots.Select(Map);
    }

    public async Task<SlotAvailabilityResponse> GetAvailabilityAsync(Guid lotId, string? type = null)
    {
        SlotType? slotType = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<SlotType>(type, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Invalid slot type '{type}'. Use: Car, Bike, Truck.");
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
            throw new ArgumentException($"Invalid slot type '{req.Type}'. Use: Car, Bike, Truck.");

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

    public async Task<SlotResponse> GetFirstAvailableAsync()
    {
        var slot = await repo.GetFirstAvailableAsync()
            ?? throw new KeyNotFoundException("No available slots found in the parking lot.");
        return Map(slot);
    }

    public async Task DeleteAsync(Guid slotId)
    {
        var slot = await repo.GetByIdAsync(slotId)
            ?? throw new KeyNotFoundException($"Slot '{slotId}' not found.");
        await repo.DeleteAsync(slot);
    }

    private static SlotResponse Map(SlotEntity s) => new(
        s.SlotId, s.LotId, s.SlotNumber,
        s.Type.ToString(), s.Status.ToString(),
        s.PricePerHour, s.CreatedAt, s.UpdatedAt);
}
