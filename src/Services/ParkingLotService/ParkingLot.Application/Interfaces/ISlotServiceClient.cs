namespace ParkingLot.Application.Interfaces;

public interface ISlotServiceClient
{
    Task BulkCreateSlotsAsync(Guid lotId, int count, decimal pricePerHour, string bearerToken);
    Task UpdateLotPricesAsync(Guid lotId, decimal newPrice, string bearerToken);
}
