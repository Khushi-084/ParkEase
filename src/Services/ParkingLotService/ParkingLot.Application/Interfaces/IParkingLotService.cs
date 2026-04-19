using ParkingLot.Application.DTOs;

namespace ParkingLot.Application.Interfaces;

public interface IParkingLotService
{
    Task<LotResponse>                CreateAsync(CreateLotRequest request);
    Task<LotResponse>                GetByIdAsync(Guid lotId);
    Task<PagedResponse<LotResponse>> GetAllAsync(int page, int pageSize, string? city, string? status);
    Task<LotResponse>                UpdateAsync(Guid lotId, UpdateLotRequest request);
    Task<LotResponse>                UpdateStatusAsync(Guid lotId, LotStatusUpdateRequest request);
    Task                             DeleteAsync(Guid lotId);
}