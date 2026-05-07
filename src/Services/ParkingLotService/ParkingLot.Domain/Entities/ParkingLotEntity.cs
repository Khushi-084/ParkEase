using ParkingLot.Domain.Enums;

namespace ParkingLot.Domain.Entities;

public class ParkingLotEntity
{
    public Guid      LotId          { get; set; } = Guid.NewGuid();
    public string    Name           { get; set; } = string.Empty;
    public string    Address        { get; set; } = string.Empty;
    public string    City           { get; set; } = string.Empty;
    public string    State          { get; set; } = string.Empty;
    public string    PinCode        { get; set; } = string.Empty;
    public double    Latitude       { get; set; }
    public double    Longitude      { get; set; }
    public int       TotalSpots     { get; set; }
    public int       AvailableSpots { get; set; }
    public decimal   PricePerHour   { get; set; }
    public LotStatus Status         { get; set; } = LotStatus.Active;
    public Guid      ManagerId      { get; set; }
    public string?   ImageUrl       { get; set; }
    public string?   Description    { get; set; }
    public DateTime  CreatedAt      { get; set; } = DateTime.UtcNow;
    public DateTime  UpdatedAt      { get; set; } = DateTime.UtcNow;
}