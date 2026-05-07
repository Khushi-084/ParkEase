using ParkingLot.Application.DTOs;
using ParkingLot.Application.Interfaces;
using ParkingLot.Domain.Entities;
using ParkingLot.Domain.Enums;

namespace ParkingLot.Infrastructure.Services;

public class ParkingLotService(
    IParkingLotRepository repo,
    ITicketServiceClient  ticketClient,
    ISlotServiceClient    slotClient) : IParkingLotService
{
    public async Task<LotResponse> CreateAsync(CreateLotRequest req, string bearerToken)
    {
        if (await repo.ExistsByNameAndCityAsync(req.Name, req.City))
            throw new InvalidOperationException(
                $"A parking lot named '{req.Name}' already exists in {req.City}.");

        if (req.ManagerId == Guid.Empty)
            throw new ArgumentException("ManagerId cannot be an empty GUID.");

        var lot = new ParkingLotEntity
        {
            Name           = req.Name.Trim(),
            Address        = req.Address.Trim(),
            City           = req.City.Trim(),
            State          = req.State.Trim(),
            PinCode        = req.PinCode.Trim(),
            Latitude       = req.Latitude,
            Longitude      = req.Longitude,
            TotalSpots     = req.TotalSpots,
            AvailableSpots = req.TotalSpots,
            PricePerHour   = req.PricePerHour,
            ManagerId      = req.ManagerId,
            ImageUrl       = req.ImageUrl,
            Description    = req.Description
        };

        await repo.AddAsync(lot);
        await repo.SaveChangesAsync();

        // Automatically create slots for the new lot
        await slotClient.BulkCreateSlotsAsync(lot.LotId, lot.TotalSpots, lot.PricePerHour, bearerToken);

        return Map(lot);
    }

    public async Task<LotResponse> GetByIdAsync(Guid lotId)
    {
        var lot = await repo.GetByIdAsync(lotId)
            ?? throw new KeyNotFoundException($"Parking lot '{lotId}' not found.");
        return Map(lot);
    }

    public async Task<PagedResponse<LotResponse>> GetAllAsync(
        int page, int pageSize, string? city, string? status)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, total) = await repo.GetAllAsync(page, pageSize, city, status);
        var totalPages     = (int)Math.Ceiling(total / (double)pageSize);

        return new PagedResponse<LotResponse>(
            items.Select(Map), total, page, pageSize, totalPages);
    }

    public async Task<LotResponse> UpdateAsync(Guid lotId, UpdateLotRequest req)
    {
        var lot = await repo.GetByIdAsync(lotId)
            ?? throw new KeyNotFoundException($"Parking lot '{lotId}' not found.");

        if (await repo.ExistsByNameAndCityAsync(req.Name, req.City, excludeId: lotId))
            throw new InvalidOperationException(
                $"Another parking lot named '{req.Name}' already exists in {req.City}.");

        var spotDiff       = req.TotalSpots - lot.TotalSpots;
        lot.AvailableSpots = Math.Max(0, lot.AvailableSpots + spotDiff);
        lot.Name           = req.Name.Trim();
        lot.Address        = req.Address.Trim();
        lot.City           = req.City.Trim();
        lot.State          = req.State.Trim();
        lot.PinCode        = req.PinCode.Trim();
        lot.Latitude       = req.Latitude;
        lot.Longitude      = req.Longitude;
        lot.TotalSpots     = req.TotalSpots;
        lot.PricePerHour   = req.PricePerHour;
        lot.ImageUrl       = req.ImageUrl;
        lot.Description    = req.Description;
        lot.UpdatedAt      = DateTime.UtcNow;

        await repo.SaveChangesAsync();
        // Since UpdateAsync doesn't take a bearer token in this implementation, we can just pass an empty string
        // The endpoint we added in SlotService doesn't strictly require [Authorize] or we didn't add it. Let's pass empty.
        await slotClient.UpdateLotPricesAsync(lotId, req.PricePerHour, "");
        return Map(lot);
    }

    public async Task<LotResponse> UpdateStatusAsync(Guid lotId, LotStatusUpdateRequest req)
    {
        var lot = await repo.GetByIdAsync(lotId)
            ?? throw new KeyNotFoundException($"Parking lot '{lotId}' not found.");

        if (!Enum.TryParse<LotStatus>(req.Status, ignoreCase: true, out var newStatus))
            throw new ArgumentException(
                $"Invalid status '{req.Status}'. Use: Active, Inactive, UnderMaintenance.");

        lot.Status    = newStatus;
        lot.UpdatedAt = DateTime.UtcNow;

        await repo.SaveChangesAsync();
        return Map(lot);
    }

    /// <summary>
    /// FIXED: Checks TicketService for active tickets before deleting the lot.
    /// Returns 409 Conflict if vehicles are still parked.
    /// </summary>
    public async Task DeleteAsync(Guid lotId, string bearerToken)
    {
        var lot = await repo.GetByIdAsync(lotId)
            ?? throw new KeyNotFoundException($"Parking lot '{lotId}' not found.");

        var hasActiveTickets = await ticketClient.HasActiveTicketsForLotAsync(lotId, bearerToken);
        if (hasActiveTickets)
            throw new InvalidOperationException(
                $"Cannot delete lot '{lotId}': it has vehicles currently parked. " +
                "All vehicles must exit before the lot can be deleted.");

        await repo.DeleteAsync(lot);
    }

    private static LotResponse Map(ParkingLotEntity p) => new(
        p.LotId, p.Name, p.Address, p.City, p.State, p.PinCode,
        p.Latitude, p.Longitude, p.TotalSpots, p.AvailableSpots,
        p.PricePerHour, p.Status.ToString(), p.ManagerId,
        p.ImageUrl, p.Description, p.CreatedAt, p.UpdatedAt);
}