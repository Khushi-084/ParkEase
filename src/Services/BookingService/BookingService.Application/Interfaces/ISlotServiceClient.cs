namespace BookingService.Application.Interfaces;

public interface ISlotServiceClient
{
    Task<bool> ReserveSlotAsync(Guid slotId, Guid correlationId);
    Task<bool> ConfirmSlotAsync(Guid slotId, Guid correlationId);
    Task<bool> ReleaseSlotAsync(Guid slotId, Guid correlationId);
}