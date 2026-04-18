namespace BuildingBlocks.Contracts.Events;

public record BookingCreatedEvent(
    Guid BookingId, Guid UserId, Guid LotId, Guid SpotId,
    DateTime StartTime, DateTime EndTime
);