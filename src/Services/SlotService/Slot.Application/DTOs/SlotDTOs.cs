using System.ComponentModel.DataAnnotations;

namespace Slot.Application.DTOs;

public record CreateSlotRequest(
    [Required(ErrorMessage = "LotId is required.")]
    Guid LotId,

    [Required(ErrorMessage = "SlotNumber is required.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "SlotNumber must be between 1 and 20 characters.")]
    [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "SlotNumber can only contain letters, digits, and hyphens.")]
    string SlotNumber,

    [Required(ErrorMessage = "Slot type is required.")]
    [RegularExpression("^(Car|Bike|Truck|EV)$",
        ErrorMessage = "Type must be one of: Car, Bike, Truck, EV.")]
    string Type,

    [Range(0.01, double.MaxValue, ErrorMessage = "PricePerHour must be greater than 0.")]
    decimal PricePerHour
);

public record UpdateSlotRequest(
    [Required(ErrorMessage = "SlotNumber is required.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "SlotNumber must be between 1 and 20 characters.")]
    [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "SlotNumber can only contain letters, digits, and hyphens.")]
    string SlotNumber,

    [Required(ErrorMessage = "Slot type is required.")]
    [RegularExpression("^(Car|Bike|Truck|EV)$",
        ErrorMessage = "Type must be one of: Car, Bike, Truck, EV.")]
    string Type,

    [Range(0.01, double.MaxValue, ErrorMessage = "PricePerHour must be greater than 0.")]
    decimal PricePerHour
);

public record SlotStatusUpdateRequest(
    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression("^(Available|Occupied|Reserved|UnderMaintenance)$",
        ErrorMessage = "Status must be Available, Occupied, Reserved, or UnderMaintenance.")]
    string Status
);

public record BulkCreateSlotRequest(
    [Required(ErrorMessage = "LotId is required.")]
    Guid LotId,

    [Required(ErrorMessage = "Slot type is required.")]
    [RegularExpression("^(Car|Bike|Truck|EV)$",
        ErrorMessage = "Type must be one of: Car, Bike, Truck, EV.")]
    string Type,

    [Range(1, 500, ErrorMessage = "Count must be between 1 and 500.")]
    int Count,

    [Required(ErrorMessage = "Prefix is required.")]
    [StringLength(5, MinimumLength = 1, ErrorMessage = "Prefix must be between 1 and 5 characters.")]
    [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Prefix can only contain alphanumeric characters.")]
    string Prefix,

    [Range(0.01, double.MaxValue, ErrorMessage = "PricePerHour must be greater than 0.")]
    decimal PricePerHour
);

public record SlotResponse(
    Guid     SlotId,
    Guid     LotId,
    string   SlotNumber,
    string   Type,
    string   Status,
    decimal  PricePerHour,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record SlotAvailabilityResponse(
    Guid   LotId,
    int    TotalSlots,
    int    AvailableSlots,
    int    OccupiedSlots,
    int    ReservedSlots,
    IEnumerable<SlotResponse> AvailableSlotDetails
);