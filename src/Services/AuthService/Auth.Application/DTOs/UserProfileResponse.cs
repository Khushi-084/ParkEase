namespace Auth.Application.DTOs;

public record UserProfileResponse(
    Guid    UserId,
    string  FullName,
    string  Email,
    string  Phone,
    string  Role,
    bool    IsActive,
    bool?   IsApproved,   // null for Driver/Admin, true/false for LotManager
    string? ProfilePicUrl,
    DateTime CreatedAt
);