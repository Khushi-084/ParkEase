using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    // GET /api/v1/admin/users
    [HttpGet("users")]
    [ProducesResponseType(typeof(IEnumerable<UserProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] string? role   = null,
        [FromQuery] bool?   active = null)
    {
        try   { return Ok(await adminService.GetAllUsersAsync(role, active)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // GET /api/v1/admin/users/pending-lotmanagers
    // Returns all LotManagers waiting for approval
    [HttpGet("users/pending-lotmanagers")]
    [ProducesResponseType(typeof(IEnumerable<UserProfileResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingLotManagers() =>
        Ok(await adminService.GetPendingLotManagersAsync());

    // GET /api/v1/admin/users/{userId}
    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        try   { return Ok(await adminService.GetUserByIdAsync(userId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // PATCH /api/v1/admin/users/{userId}/approve
    // Admin approves a LotManager
    [HttpPatch("users/{userId:guid}/approve")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveLotManager(Guid userId)
    {
        try   { return Ok(await adminService.ApproveLotManagerAsync(userId)); }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // PATCH /api/v1/admin/users/{userId}/reject
    // Admin rejects/revokes a LotManager approval
    [HttpPatch("users/{userId:guid}/reject")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectLotManager(Guid userId)
    {
        try   { return Ok(await adminService.RejectLotManagerAsync(userId)); }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // PATCH /api/v1/admin/users/{userId}/role
    [HttpPatch("users/{userId:guid}/role")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(Guid userId, [FromBody] ChangeUserRoleRequest req)
    {
        try   { return Ok(await adminService.ChangeUserRoleAsync(userId, req)); }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (ArgumentException ex)         { return BadRequest(new { error = ex.Message }); }
    }

    // PATCH /api/v1/admin/users/{userId}/activate
    [HttpPatch("users/{userId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(Guid userId)
    {
        try
        {
            await adminService.SetUserActiveStatusAsync(userId, true);
            return Ok(new { message = "User activated successfully." });
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // PATCH /api/v1/admin/users/{userId}/deactivate
    [HttpPatch("users/{userId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(Guid userId)
    {
        try
        {
            await adminService.SetUserActiveStatusAsync(userId, false);
            return Ok(new { message = "User deactivated successfully." });
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // DELETE /api/v1/admin/users/{userId}
    [HttpDelete("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        try
        {
            await adminService.DeleteUserAsync(userId);
            return Ok(new { message = "User deleted successfully." });
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}