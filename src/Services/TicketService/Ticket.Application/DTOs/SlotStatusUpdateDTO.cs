namespace Ticket.Application.DTOs;

public class SlotStatusUpdateDto
{
    public string Status { get; set; } = string.Empty;

    public SlotStatusUpdateDto() { }
    
    public SlotStatusUpdateDto(string status)
    {
        Status = status;
    }
}