namespace ParkingLot.Application.Interfaces;

/// <summary>
/// FIXED: Allows ParkingLotService to call TicketService and check whether
/// any active tickets exist for a lot before allowing deletion.
/// </summary>
public interface ITicketServiceClient
{
    /// <summary>
    /// Returns true if there is at least one active (non-completed) ticket
    /// for any slot belonging to the given lot.
    /// </summary>
    Task<bool> HasActiveTicketsForLotAsync(Guid lotId, string bearerToken);
}