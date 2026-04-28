using ParkingLot.Application.DTOs;

namespace ParkingLot.Application.Interfaces;

public interface IParkingLotService
{
    Task<LotResponse>                CreateAsync(CreateLotRequest request, string bearerToken);
    Task<LotResponse>                GetByIdAsync(Guid lotId);
    Task<PagedResponse<LotResponse>> GetAllAsync(int page, int pageSize, string? city, string? status);
    Task<LotResponse>                UpdateAsync(Guid lotId, UpdateLotRequest request);
    Task<LotResponse>                UpdateStatusAsync(Guid lotId, LotStatusUpdateRequest request);

    /// <summary>
    /// FIXED: bearerToken is forwarded to TicketService so it can check
    /// for active tickets before allowing deletion.
    /// </summary>
    Task DeleteAsync(Guid lotId, string bearerToken);
}