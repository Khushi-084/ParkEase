using Microsoft.EntityFrameworkCore;
using Slot.Application.Interfaces;
using Slot.Domain.Entities;
using Slot.Domain.Enums;
using Slot.Infrastructure.Persistence;

namespace Slot.Infrastructure.Repositories;

public class SlotRepository(SlotDbContext db) : ISlotRepository
{
    public Task<SlotEntity?> GetByIdAsync(Guid slotId) =>
        db.Slots.FirstOrDefaultAsync(s => s.SlotId == slotId);

    public async Task<IEnumerable<SlotEntity>> GetByLotIdAsync(Guid lotId) =>
        await db.Slots
            .Where(s => s.LotId == lotId)
            .OrderBy(s => s.SlotNumber)
            .ToListAsync();

    public async Task<IEnumerable<SlotEntity>> GetAvailableByLotIdAsync(Guid lotId, SlotType? type = null)
    {
        var query = db.Slots
            .Where(s => s.LotId == lotId && s.Status == SlotStatus.Available);

        if (type.HasValue)
            query = query.Where(s => s.Type == type.Value);

        return await query.OrderBy(s => s.SlotNumber).ToListAsync();
    }

    public Task<bool> ExistsBySlotNumberAsync(Guid lotId, string slotNumber, Guid? excludeId = null)
    {
        var query = db.Slots.Where(s =>
            s.LotId == lotId &&
            s.SlotNumber.ToLower() == slotNumber.ToLower());

        if (excludeId.HasValue)
            query = query.Where(s => s.SlotId != excludeId.Value);

        return query.AnyAsync();
    }

    public async Task AddAsync(SlotEntity slot) =>
        await db.Slots.AddAsync(slot);

    public async Task AddRangeAsync(IEnumerable<SlotEntity> slots) =>
        await db.Slots.AddRangeAsync(slots);

    public async Task DeleteAsync(SlotEntity slot)
    {
        db.Slots.Remove(slot);
        await db.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}