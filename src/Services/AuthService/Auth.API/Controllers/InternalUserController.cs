using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers;

[ApiController]
[Route("api/v1/internal/users")]
public class InternalUserController(IAdminService adminService) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserInternal(Guid userId)
    {
        try 
        {
            var user = await adminService.GetUserByIdAsync(userId);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
