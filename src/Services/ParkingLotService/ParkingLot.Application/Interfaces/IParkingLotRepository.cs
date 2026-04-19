using ParkingLot.Domain.Entities;

namespace ParkingLot.Application.Interfaces;

public interface IParkingLotRepository
{
    Task<ParkingLotEntity?>  GetByIdAsync(Guid lotId);
    Task<(IEnumerable<ParkingLotEntity> Items, int Total)> GetAllAsync(
        int page, int pageSize, string? city, string? status);
    Task<bool>               ExistsByNameAndCityAsync(string name, string city, Guid? excludeId = null);
    Task                     AddAsync(ParkingLotEntity lot);
    Task                     SaveChangesAsync();
    Task                     DeleteAsync(ParkingLotEntity lot);
}