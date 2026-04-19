using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public record RegisterRequest(
    [Required] string FullName,
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password,
    [Required] string Phone,
    [Required] string Role
);