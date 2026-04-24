using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Repositories;

public class BookingRepository(BookingDbContext db) : IBookingRepository
{
    public Task<BookingEntity?> GetByIdAsync(Guid id) =>
        db.Bookings.FirstOrDefaultAsync(b => b.Id == id);

    public Task<BookingEntity?> GetByCorrelationIdAsync(Guid correlationId) =>
        db.Bookings.FirstOrDefaultAsync(b => b.CorrelationId == correlationId);

    public async Task<IEnumerable<BookingEntity>> GetByUserIdAsync(Guid userId) =>
        await db.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<BookingEntity> AddAsync(BookingEntity booking)
    {
        await db.Bookings.AddAsync(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    public async Task<BookingEntity> UpdateAsync(BookingEntity booking)
    {
        db.Bookings.Update(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}