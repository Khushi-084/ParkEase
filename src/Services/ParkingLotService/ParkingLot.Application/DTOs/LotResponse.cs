namespace ParkingLot.Application.DTOs;

public record LotResponse(
    Guid     LotId,
    string   Name,
    string   Address,
    string   City,
    string   State,
    string   PinCode,
    double   Latitude,
    double   Longitude,
    int      TotalSpots,
    int      AvailableSpots,
    decimal  PricePerHour,
    string   Status,
    Guid     ManagerId,
    string?  ImageUrl,
    string?  Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
);