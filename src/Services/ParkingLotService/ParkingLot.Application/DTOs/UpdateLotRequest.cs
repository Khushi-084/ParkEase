using System.ComponentModel.DataAnnotations;

namespace ParkingLot.Application.DTOs;

public record UpdateLotRequest(
    [Required][MaxLength(150)] string  Name,
    [Required][MaxLength(300)] string  Address,
    [Required][MaxLength(100)] string  City,
    [Required][MaxLength(100)] string  State,
    [Required][MaxLength(10)]  string  PinCode,
    [Range(-90, 90)]           double  Latitude,
    [Range(-180, 180)]         double  Longitude,
    [Range(1, 10000)]          int     TotalSpots,
    [Range(0.01, 100000)]      decimal PricePerHour,
    string? ImageUrl,
    string? Description
);