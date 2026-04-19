namespace Auth.Application.DTOs;

public record UserProfileResponse(
    Guid     UserId,
    string   FullName,
    string   Email,
    string   Phone,
    string   Role,
    bool     IsActive,
    string?  ProfilePicUrl,
    DateTime CreatedAt
);