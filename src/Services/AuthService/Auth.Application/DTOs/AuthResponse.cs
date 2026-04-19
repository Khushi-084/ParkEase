namespace Auth.Application.DTOs;

public record AuthResponse(
    string Token,
    string Role,
    Guid   UserId,
    string FullName,
    string Email
);