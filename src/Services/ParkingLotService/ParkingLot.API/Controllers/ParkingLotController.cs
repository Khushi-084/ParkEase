using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkingLot.Application.DTOs;
using ParkingLot.Application.Interfaces;

namespace ParkingLot.API.Controllers;

[ApiController]
[Route("api/v1/lots")]
[Produces("application/json")]
public class ParkingLotController(IParkingLotService lotService) : ControllerBase
{
    // POST /api/v1/lots
    [Authorize(Roles = "Admin,LotManager")]
    [HttpPost]
    [ProducesResponseType(typeof(LotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]   
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateLotRequest req)
    {
        try
        {
            var result = await lotService.CreateAsync(req);
            return CreatedAtAction(nameof(GetById), new { lotId = result.LotId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    // GET /api/v1/lots
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<LotResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 10,
        [FromQuery] string? city     = null,
        [FromQuery] string? status   = null)
    {
        var result = await lotService.GetAllAsync(page, pageSize, city, status);
        return Ok(result);
    }

    // GET /api/v1/lots/{lotId}
    [HttpGet("{lotId:guid}")]
    [ProducesResponseType(typeof(LotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid lotId)
    {
        try
        {
            var result = await lotService.GetByIdAsync(lotId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // PUT /api/v1/lots/{lotId}
    [Authorize(Roles = "Admin,LotManager")]
    [HttpPut("{lotId:guid}")]
    [ProducesResponseType(typeof(LotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]  
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid lotId, [FromBody] UpdateLotRequest req)
    {
        try
        {
            var result = await lotService.UpdateAsync(lotId, req);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // PATCH /api/v1/lots/{lotId}/status
    [Authorize(Roles = "Admin")]
    [HttpPatch("{lotId:guid}/status")]
    [ProducesResponseType(typeof(LotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(Guid lotId, [FromBody] LotStatusUpdateRequest req)
    {
        try
        {
            var result = await lotService.UpdateStatusAsync(lotId, req);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // DELETE /api/v1/lots/{lotId}
    [Authorize(Roles = "Admin")]
    [HttpDelete("{lotId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid lotId)
    {
        try
        {
            await lotService.DeleteAsync(lotId);
            return Ok(new { message = "Parking lot deleted successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}