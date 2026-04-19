using System.ComponentModel.DataAnnotations;

namespace Auth.Application.DTOs;

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);