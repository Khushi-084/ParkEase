using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public record UpdateProfileRequest(
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Full name can only contain letters and spaces.")]
    string FullName,

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
    string Phone,

    [Url(ErrorMessage = "Invalid URL format for profile picture.")]
    string? ProfilePicUrl
);