using BookingService.Domain.Entities;

namespace BookingService.Application.Interfaces;

public interface IBookingRepository
{
    Task<BookingEntity?>              GetByIdAsync(Guid id);
    Task<BookingEntity?>              GetByCorrelationIdAsync(Guid correlationId);
    Task<IEnumerable<BookingEntity>>  GetByUserIdAsync(Guid userId);
    Task<BookingEntity>               AddAsync(BookingEntity booking);
    Task<BookingEntity>               UpdateAsync(BookingEntity booking);
    Task                              SaveChangesAsync();
}