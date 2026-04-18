using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required][MinLength(6)] string NewPassword
);