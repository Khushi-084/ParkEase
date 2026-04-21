using Auth.Application.DTOs;

namespace Auth.Application.Interfaces;

public interface IAdminService
{
    Task<IEnumerable<UserProfileResponse>> GetAllUsersAsync(string? role, bool? active);
    Task<IEnumerable<UserProfileResponse>> GetPendingLotManagersAsync();
    Task<UserProfileResponse>              GetUserByIdAsync(Guid userId);
    Task<UserProfileResponse>              ApproveLotManagerAsync(Guid userId);
    Task<UserProfileResponse>              RejectLotManagerAsync(Guid userId);
    Task<UserProfileResponse>              ChangeUserRoleAsync(Guid userId, ChangeUserRoleRequest req);
    Task                                   SetUserActiveStatusAsync(Guid userId, bool isActive);
    Task                                   DeleteUserAsync(Guid userId);
}