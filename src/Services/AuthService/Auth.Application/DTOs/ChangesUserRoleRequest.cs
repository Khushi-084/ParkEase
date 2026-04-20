using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public record ChangeUserRoleRequest(
    [Required]
    [RegularExpression("^(Driver|LotManager)$",
        ErrorMessage = "Role must be either 'Driver' or 'LotManager'.")]
    string Role
);