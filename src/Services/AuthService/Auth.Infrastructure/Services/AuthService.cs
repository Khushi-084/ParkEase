using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Services;

public class AuthService(IUserRepository userRepo, IConfiguration config) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        if (await userRepo.ExistsByEmailAsync(req.Email))
            throw new InvalidOperationException("Email is already registered.");

        if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var role))
            throw new ArgumentException($"Invalid role. Valid roles: Driver, LotManager.");

        if (role == UserRole.Admin)
            throw new ArgumentException("Admin accounts cannot be created through public registration.");

        var user = new User
        {
            FullName     = req.FullName.Trim(),
            Email        = req.Email.ToLower().Trim(),
            Phone        = req.Phone.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role         = role,
            // ✅ LotManager starts as unapproved — needs admin approval
            // Driver gets null (approval not applicable)
            IsApproved   = role == UserRole.LotManager ? false : null
        };

        await userRepo.AddAsync(user);
        await userRepo.SaveChangesAsync();
        return MapToResponse(GenerateToken(user), user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await userRepo.FindByEmailAsync(req.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Your account has been deactivated. Contact admin.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        // ✅ LotManager blocked until admin approves
        if (user.Role == UserRole.LotManager && user.IsApproved == false)
            throw new UnauthorizedAccessException(
                "Your LotManager account is pending admin approval. Please wait for approval.");

        return MapToResponse(GenerateToken(user), user);
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId)
    {
        var user = await userRepo.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        return MapToProfile(user);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest req)
    {
        var user = await userRepo.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.FullName      = req.FullName.Trim();
        user.Phone         = req.Phone.Trim();
        user.ProfilePicUrl = req.ProfilePicUrl;
        await userRepo.SaveChangesAsync();
        return MapToProfile(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest req)
    {
        var user = await userRepo.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await userRepo.SaveChangesAsync();
    }

    public async Task DeactivateAccountAsync(Guid userId)
    {
        var user = await userRepo.FindByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        user.IsActive = false;
        await userRepo.SaveChangesAsync();
    }

    private string GenerateToken(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Role,           user.Role.ToString()),
            new Claim(ClaimTypes.Name,           user.FullName),
            new Claim("isApproved",              user.IsApproved?.ToString() ?? "")
        };
        var token = new JwtSecurityToken(
            issuer:             config["Jwt:Issuer"],
            audience:           config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static AuthResponse MapToResponse(string token, User u) =>
        new(token, u.Role.ToString(), u.UserId, u.FullName, u.Email, u.IsApproved);

    private static UserProfileResponse MapToProfile(User u) => new(
        u.UserId, u.FullName, u.Email, u.Phone,
        u.Role.ToString(), u.IsActive, u.IsApproved, u.ProfilePicUrl, u.CreatedAt);
}