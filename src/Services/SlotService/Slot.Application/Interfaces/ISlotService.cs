using Slot.Application.DTOs;

namespace Slot.Application.Interfaces;

public interface ISlotService
{
    // ── Existing ──────────────────────────────────────────────────────────────
    Task<SlotResponse>              CreateAsync(CreateSlotRequest req);
    Task<IEnumerable<SlotResponse>> BulkCreateAsync(BulkCreateSlotRequest req);
    Task<SlotResponse>              GetByIdAsync(Guid slotId);
    Task<IEnumerable<SlotResponse>> GetByLotIdAsync(Guid lotId);
    Task<SlotAvailabilityResponse>  GetAvailabilityAsync(Guid lotId, string? type = null);
    Task<SlotResponse>              UpdateAsync(Guid slotId, UpdateSlotRequest req);
    Task<SlotResponse>              UpdateStatusAsync(Guid slotId, SlotStatusUpdateRequest req);
    Task<SlotResponse>              GetFirstAvailableAsync(string? type = null);
    Task                            DeleteAsync(Guid slotId);

    // ── NEW: Booking Saga operations ──────────────────────────────────────────

    /// <summary>
    /// Reserve a slot for a pending booking.
    /// Transitions: Available → Reserved.
    /// Throws InvalidOperationException if slot is not Available.
    /// </summary>
    Task<SlotResponse> ReserveSlotAsync(Guid slotId);

    /// <summary>
    /// Confirm a reserved slot after payment succeeds.
    /// Transitions: Reserved → Occupied.
    /// Throws InvalidOperationException if slot is not Reserved.
    /// </summary>
    Task<SlotResponse> ConfirmSlotAsync(Guid slotId);

    /// <summary>
    /// Release a reserved slot after payment fails (saga compensation).
    /// Transitions: Reserved → Available.
    /// Throws InvalidOperationException if slot is not Reserved.
    /// </summary>
    Task<SlotResponse> ReleaseSlotAsync(Guid slotId);
}