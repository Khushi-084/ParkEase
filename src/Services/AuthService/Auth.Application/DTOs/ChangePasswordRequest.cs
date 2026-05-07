using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public record ChangePasswordRequest(
    [Required(ErrorMessage = "Current password is required.")]
    string CurrentPassword,

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number, and one special character.")]
    string NewPassword
);