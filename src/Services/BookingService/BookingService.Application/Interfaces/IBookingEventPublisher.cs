namespace BookingService.Application.Interfaces;

public interface IBookingEventPublisher
{
    Task PublishBookingConfirmedAsync(Guid userId, string email, Guid bookingId, string lotName);
}
