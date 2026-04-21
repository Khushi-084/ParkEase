using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slot.Application.DTOs;
using Slot.Application.Interfaces;

namespace Slot.API.Controllers;

[ApiController]
[Route("api/v1/slots")]
[Produces("application/json")]
public class SlotController(ISlotService slotService) : ControllerBase
{
    // POST /api/v1/slots
    [Authorize(Roles = "Admin,LotManager")]
    [HttpPost]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateSlotRequest req)
    {
        try
        {
            var result = await slotService.CreateAsync(req);
            return CreatedAtAction(nameof(GetById), new { slotId = result.SlotId }, result);
        }
        catch (ArgumentException ex)        { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // POST /api/v1/slots/bulk
    [Authorize(Roles = "Admin,LotManager")]
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(IEnumerable<SlotResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateSlotRequest req)
    {
        try
        {
            var result = await slotService.BulkCreateAsync(req);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)        { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // GET /api/v1/slots/{slotId}
    [HttpGet("{slotId:guid}")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid slotId)
    {
        try   { return Ok(await slotService.GetByIdAsync(slotId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // GET /api/v1/slots/lot/{lotId}
    [HttpGet("lot/{lotId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SlotResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByLot(Guid lotId) =>
        Ok(await slotService.GetByLotIdAsync(lotId));

    // GET /api/v1/slots/available/first
    [HttpGet("available/first")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFirstAvailable()
    {
        try   { return Ok(await slotService.GetFirstAvailableAsync()); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // GET /api/v1/slots/lot/{lotId}/availability?type=Car
    [HttpGet("lot/{lotId:guid}/availability")]
    [ProducesResponseType(typeof(SlotAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAvailability(Guid lotId, [FromQuery] string? type = null)
    {
        try   { return Ok(await slotService.GetAvailabilityAsync(lotId, type)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // PUT /api/v1/slots/{slotId}
    [Authorize(Roles = "Admin,LotManager")]
    [HttpPut("{slotId:guid}")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid slotId, [FromBody] UpdateSlotRequest req)
    {
        try
        {
            return Ok(await slotService.UpdateAsync(slotId, req));
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex)         { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // PATCH /api/v1/slots/{slotId}/status
    [Authorize(Roles = "Admin,LotManager")]
    [HttpPatch("{slotId:guid}/status")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid slotId, [FromBody] SlotStatusUpdateRequest req)
    {
        try
        {
            return Ok(await slotService.UpdateStatusAsync(slotId, req));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex)    { return BadRequest(new { error = ex.Message }); }
    }

    // DELETE /api/v1/slots/{slotId}
    [Authorize(Roles = "Admin")]
    [HttpDelete("{slotId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid slotId)
    {
        try
        {
            await slotService.DeleteAsync(slotId);
            return Ok(new { message = "Slot deleted successfully." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}