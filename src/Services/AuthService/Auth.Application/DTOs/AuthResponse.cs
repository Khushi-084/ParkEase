namespace Auth.Application.DTOs;

public record AuthResponse(
    string  Token,
    string  Role,
    Guid    UserId,
    string  FullName,
    string  Email,
    bool?   IsApproved  // null for Driver/Admin, true/false for LotManager
);