using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public record UpdateProfileRequest(
    [Required] string FullName,
    [Required] string Phone,
    string? ProfilePicUrl
);