namespace Ticket.Application.DTOs;

public class SlotDTO
{
    public Guid SlotId { get; set; }
    public string SlotNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid LotId { get; set; }
    public decimal PricePerHour { get; set; }
}