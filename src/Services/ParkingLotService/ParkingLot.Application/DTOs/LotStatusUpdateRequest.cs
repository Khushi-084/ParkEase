using System.ComponentModel.DataAnnotations;

namespace ParkingLot.Application.DTOs;

public record LotStatusUpdateRequest(
    [Required]
    [RegularExpression(                                        // ✅ ADDED
        "^(Active|Inactive|UnderMaintenance)$",
        ErrorMessage = "Status must be Active, Inactive, or UnderMaintenance.")]
    string Status
);