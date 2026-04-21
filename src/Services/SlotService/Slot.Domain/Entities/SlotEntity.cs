using Slot.Domain.Enums;

namespace Slot.Domain.Entities;

public class SlotEntity
{
    public Guid       SlotId      { get; set; } = Guid.NewGuid();
    public Guid       LotId       { get; set; }           // FK to ParkingLot
    public string     SlotNumber  { get; set; } = string.Empty; // e.g. A-01, B-12
    public SlotType   Type        { get; set; } = SlotType.Car;
    public SlotStatus Status      { get; set; } = SlotStatus.Available;
    public decimal    PricePerHour { get; set; }
    public DateTime   CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime   UpdatedAt   { get; set; } = DateTime.UtcNow;
}