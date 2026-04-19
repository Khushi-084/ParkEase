using Microsoft.EntityFrameworkCore;
using ParkingLot.Application.Interfaces;
using ParkingLot.Domain.Entities;
using ParkingLot.Domain.Enums;
using ParkingLot.Infrastructure.Persistence;

namespace ParkingLot.Infrastructure.Repositories;

public class ParkingLotRepository(ParkingLotDbContext db) : IParkingLotRepository
{
    public Task<ParkingLotEntity?> GetByIdAsync(Guid lotId) =>
        db.ParkingLots.FirstOrDefaultAsync(p => p.LotId == lotId);

    public async Task<(IEnumerable<ParkingLotEntity> Items, int Total)> GetAllAsync(
        int page, int pageSize, string? city, string? status)
    {
        var query = db.ParkingLots.AsQueryable();

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.City.ToLower().Contains(city.ToLower()));

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<LotStatus>(status, ignoreCase: true, out var lotStatus))
            query = query.Where(p => p.Status == lotStatus);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public Task<bool> ExistsByNameAndCityAsync(string name, string city, Guid? excludeId = null)
    {
        var query = db.ParkingLots.Where(p =>
            p.Name.ToLower() == name.ToLower() &&
            p.City.ToLower() == city.ToLower());

        if (excludeId.HasValue)
            query = query.Where(p => p.LotId != excludeId.Value);

        return query.AnyAsync();
    }

    public async Task AddAsync(ParkingLotEntity lot) =>
        await db.ParkingLots.AddAsync(lot);

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    // ✅ FIXED — now truly async and self-contained; call SaveChangesAsync() after this in service
    public async Task DeleteAsync(ParkingLotEntity lot)
    {
        db.ParkingLots.Remove(lot);
        await db.SaveChangesAsync();
    }
}