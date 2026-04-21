using Slot.Domain.Entities;
using Slot.Domain.Enums;

namespace Slot.Application.Interfaces;

public interface ISlotRepository
{
    Task<SlotEntity?>               GetByIdAsync(Guid slotId);
    Task<IEnumerable<SlotEntity>>   GetByLotIdAsync(Guid lotId);
    Task<IEnumerable<SlotEntity>>   GetAvailableByLotIdAsync(Guid lotId, SlotType? type = null);
    Task<bool>                      ExistsBySlotNumberAsync(Guid lotId, string slotNumber, Guid? excludeId = null);
    Task<SlotEntity?>               GetFirstAvailableAsync();
    Task                            AddAsync(SlotEntity slot);
    Task                            AddRangeAsync(IEnumerable<SlotEntity> slots);
    Task                            DeleteAsync(SlotEntity slot);
    Task                            SaveChangesAsync();
}