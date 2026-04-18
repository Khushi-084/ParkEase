using Auth.Application.DTOs;

namespace Auth.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse>        RegisterAsync(RegisterRequest request);
    Task<AuthResponse>        LoginAsync(LoginRequest request);
    Task<UserProfileResponse> GetProfileAsync(Guid userId);
    Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task                      ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task                      DeactivateAccountAsync(Guid userId);
}