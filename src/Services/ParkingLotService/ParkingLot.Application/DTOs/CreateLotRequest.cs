using System.ComponentModel.DataAnnotations;

namespace ParkingLot.Application.DTOs;

public record CreateLotRequest(
    [Required(ErrorMessage = "Lot name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 150 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-,&'.]+$", ErrorMessage = "Name contains invalid characters.")]
    string Name,

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 300 characters.")]
    string Address,

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters.")]
    [RegularExpression(@"^[a-zA-Z\s\-]+$", ErrorMessage = "City can only contain letters, spaces, and hyphens.")]
    string City,

    [Required(ErrorMessage = "State is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "State must be between 2 and 100 characters.")]
    [RegularExpression(@"^[a-zA-Z\s\-]+$", ErrorMessage = "State can only contain letters, spaces, and hyphens.")]
    string State,

    [Required(ErrorMessage = "PinCode is required.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "PinCode must be a 6-digit number.")]
    string PinCode,

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    double Latitude,

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    double Longitude,

    [Range(1, 10000, ErrorMessage = "TotalSpots must be between 1 and 10000.")]
    int TotalSpots,

    [Range(0.01, 100000, ErrorMessage = "PricePerHour must be between 0.01 and 100000.")]
    decimal PricePerHour,

    [Required(ErrorMessage = "ManagerId is required.")]
    Guid ManagerId,

    [Url(ErrorMessage = "ImageUrl must be a valid URL.")]
    string? ImageUrl,

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    string? Description
);