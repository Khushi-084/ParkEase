using Slot.Domain.Entities;
using Slot.Domain.Enums;

namespace Slot.Application.Interfaces;

public interface ISlotRepository
{
    Task<SlotEntity?>               GetByIdAsync(Guid slotId);
    Task<IEnumerable<SlotEntity>>   GetByLotIdAsync(Guid lotId);
    Task<IEnumerable<SlotEntity>>   GetAvailableByLotIdAsync(Guid lotId, SlotType? type = null);
    Task<IEnumerable<string>>       GetExistingSlotNumbersAsync(Guid lotId);
    Task<bool>                      ExistsBySlotNumberAsync(Guid lotId, string slotNumber, Guid? excludeId = null);

    /// <summary>
    /// FIXED: Optional SlotType filter — used by TicketService to allocate the
    /// right slot type (e.g. EV) on vehicle entry.
    /// </summary>
    Task<SlotEntity?>               GetFirstAvailableAsync(SlotType? type = null);

    Task                            AddAsync(SlotEntity slot);
    Task                            AddRangeAsync(IEnumerable<SlotEntity> slots);
    Task                            DeleteAsync(SlotEntity slot);
    Task                            SaveChangesAsync();
}