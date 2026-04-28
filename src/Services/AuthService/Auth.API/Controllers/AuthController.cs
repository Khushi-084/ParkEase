using System.Security.Claims;
using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController(IAuthService authService) : ControllerBase
{
    // ── POST /api/v1/auth/register ──────────────────────────────────────────
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        try
        {
            var result = await authService.RegisterAsync(req);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── POST /api/v1/auth/login ─────────────────────────────────────────────
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            var result = await authService.LoginAsync(req);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    // ── GET /api/v1/auth/profile ────────────────────────────────────────────
    [Authorize]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await authService.GetProfileAsync(userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    // ── PUT /api/v1/auth/profile ────────────────────────────────────────────
    [Authorize]
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userId = GetCurrentUserId();
        var result = await authService.UpdateProfileAsync(userId, req);
        return Ok(result);
    }

    // ── PUT /api/v1/auth/password ───────────────────────────────────────────
    [Authorize]
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        try
        {
            var userId = GetCurrentUserId();
            await authService.ChangePasswordAsync(userId, req);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    // ── DELETE /api/v1/auth/deactivate ──────────────────────────────────────
    [Authorize]
    [HttpDelete("deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Deactivate()
    {
        var userId = GetCurrentUserId();
        await authService.DeactivateAccountAsync(userId);
        return Ok(new { message = "Account deactivated successfully." });
    }

    // ── Admin only: GET /api/v1/auth/admin/test ─────────────────────────────
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminOnly() =>
        Ok(new { message = "You are an Admin!", userId = GetCurrentUserId() });

    // ── Helper ──────────────────────────────────────────────────────────────
    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Token missing userId claim."));
}