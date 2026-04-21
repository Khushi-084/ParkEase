using Slot.Application.DTOs;

namespace Slot.Application.Interfaces;

public interface ISlotService
{
    Task<SlotResponse>              CreateAsync(CreateSlotRequest req);
    Task<IEnumerable<SlotResponse>> BulkCreateAsync(BulkCreateSlotRequest req);
    Task<SlotResponse>              GetByIdAsync(Guid slotId);
    Task<IEnumerable<SlotResponse>> GetByLotIdAsync(Guid lotId);
    Task<SlotAvailabilityResponse>  GetAvailabilityAsync(Guid lotId, string? type = null);
    Task<SlotResponse>              UpdateAsync(Guid slotId, UpdateSlotRequest req);
    Task<SlotResponse>              UpdateStatusAsync(Guid slotId, SlotStatusUpdateRequest req);
    Task<SlotResponse>              GetFirstAvailableAsync();
    Task                            DeleteAsync(Guid slotId);
}