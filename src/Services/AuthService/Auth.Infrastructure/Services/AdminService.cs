using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Enums;

namespace Auth.Infrastructure.Services;

public class AdminService(
    IUserRepository userRepo,
    IAuthEventPublisher eventPublisher) : IAdminService
{
    public async Task<IEnumerable<UserProfileResponse>> GetAllUsersAsync(string? role, bool? active)
    {
        UserRole? userRole = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Invalid role '{role}'. Use: Driver, LotManager, Admin.");
            userRole = parsed;
        }
        var users = await userRepo.GetAllUsersAsync(userRole, active);
        return users.Select(MapToProfile);
    }

    //  Returns only LotManagers waiting for approval
    public async Task<IEnumerable<UserProfileResponse>> GetPendingLotManagersAsync()
    {
        var users = await userRepo.GetAllUsersAsync(UserRole.LotManager, null);
        return users.Where(u => u.IsApproved == false).Select(MapToProfile);
    }

    public async Task<UserProfileResponse> GetUserByIdAsync(Guid userId)
    {
        var user = await userRepo.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");
        return MapToProfile(user);
    }

    //  Admin approves a LotManager
    public async Task<UserProfileResponse> ApproveLotManagerAsync(Guid userId)
    {
        var user = await userRepo.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        if (user.Role != UserRole.LotManager)
            throw new InvalidOperationException("Only LotManager accounts can be approved.");

        if (user.IsApproved == true)
            throw new InvalidOperationException("This LotManager is already approved.");

        user.IsApproved = true;
        await userRepo.SaveChangesAsync();

        await eventPublisher.PublishLotManagerApprovedAsync(user.UserId, user.Email, user.FullName);

        return MapToProfile(user);
    }

    // Admin rejects a LotManager (sets back to unapproved)
    public async Task<UserProfileResponse> RejectLotManagerAsync(Guid userId)
    {
        var user = await userRepo.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        if (user.Role != UserRole.LotManager)
            throw new InvalidOperationException("Only LotManager accounts can be rejected.");

        user.IsApproved = false;
        await userRepo.SaveChangesAsync();
        return MapToProfile(user);
    }

    public async Task<UserProfileResponse> ChangeUserRoleAsync(Guid userId, ChangeUserRoleRequest req)
    {
        var user = await userRepo.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        if (user.Role == UserRole.Admin)
            throw new InvalidOperationException("Admin role cannot be changed.");

        if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var newRole))
            throw new ArgumentException($"Invalid role '{req.Role}'. Use: Driver, LotManager.");

        user.Role = newRole;
        // Reset approval when role changes
        user.IsApproved = newRole == UserRole.LotManager ? false : null;
        await userRepo.SaveChangesAsync();
        return MapToProfile(user);
    }

    public async Task SetUserActiveStatusAsync(Guid userId, bool isActive)
    {
        var user = await userRepo.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        if (user.Role == UserRole.Admin)
            throw new InvalidOperationException("Admin account status cannot be changed via API.");

        user.IsActive = isActive;
        await userRepo.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await userRepo.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException($"User '{userId}' not found.");

        if (user.Role == UserRole.Admin)
            throw new InvalidOperationException("Admin account cannot be deleted via API.");

        await userRepo.DeleteAsync(user);
    }

    private static UserProfileResponse MapToProfile(User u) => new(
        u.UserId, u.FullName, u.Email, u.Phone,
        u.Role.ToString(), u.IsActive, u.IsApproved, u.ProfilePicUrl, u.CreatedAt);
}