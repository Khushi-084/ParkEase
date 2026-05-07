using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slot.Application.DTOs;
using Slot.Application.Interfaces;

namespace Slot.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/slots")]
[Produces("application/json")]
public class SlotController(ISlotService slotService) : ControllerBase
{
    // ── Existing: POST /api/slots ─────────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSlot([FromBody] CreateSlotRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var slot = await slotService.CreateAsync(request);
            return CreatedAtAction(nameof(GetSlot), new { slotId = slot.SlotId }, slot);
        }
        catch (ArgumentException ex)         { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // ── Existing: POST /api/slots/bulk ────────────────────────────────────────

    [HttpPost("bulk")]
    [ProducesResponseType(typeof(IEnumerable<SlotResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCreateSlots([FromBody] BulkCreateSlotRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var slots = await slotService.BulkCreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, slots);
        }
        catch (ArgumentException ex)         { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // ── Existing: GET /api/slots/{slotId} ────────────────────────────────────

    [HttpGet("{slotId:guid}")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSlot(Guid slotId)
    {
        try   { return Ok(await slotService.GetByIdAsync(slotId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── Existing: GET /api/slots/lot/{lotId} ─────────────────────────────────

    [HttpGet("lot/{lotId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<SlotResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSlotsByLot(Guid lotId)
        => Ok(await slotService.GetByLotIdAsync(lotId));

    // ── NEW: PUT /api/slots/lot/{lotId}/price ────────────────────────────────

    [HttpPut("lot/{lotId:guid}/price")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePricesByLot(Guid lotId, [FromBody] decimal newPrice)
    {
        await slotService.UpdatePricesByLotAsync(lotId, newPrice);
        return Ok(new { message = "Prices updated successfully." });
    }

    // ── Existing: GET /api/slots/lot/{lotId}/availability ────────────────────

    [HttpGet("lot/{lotId:guid}/availability")]
    [ProducesResponseType(typeof(SlotAvailabilityResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailability(Guid lotId, [FromQuery] string? type = null)
    {
        try   { return Ok(await slotService.GetAvailabilityAsync(lotId, type)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Existing: GET /api/slots/available ───────────────────────────────────

    [HttpGet("available")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFirstAvailable([FromQuery] string? type = null)
    {
        try   { return Ok(await slotService.GetFirstAvailableAsync(type)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex)    { return BadRequest(new { error = ex.Message }); }
    }

    // ── Existing: PUT /api/slots/{slotId} ────────────────────────────────────

    [HttpPut("{slotId:guid}")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSlot(Guid slotId, [FromBody] UpdateSlotRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try   { return Ok(await slotService.UpdateAsync(slotId, request)); }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex)         { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // ── Existing: PATCH /api/slots/{slotId}/status ───────────────────────────

    [HttpPatch("{slotId:guid}/status")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSlotStatus(
        Guid slotId, [FromBody] SlotStatusUpdateRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try   { return Ok(await slotService.UpdateStatusAsync(slotId, request)); }
        catch (KeyNotFoundException ex)  { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex)     { return BadRequest(new { error = ex.Message }); }
    }

    // ── Existing: DELETE /api/slots/{slotId} ─────────────────────────────────

    [HttpDelete("{slotId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSlot(Guid slotId)
    {
        try   { await slotService.DeleteAsync(slotId); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── NEW: POST /api/slots/{slotId}/reserve ────────────────────────────────
    // Called by Booking Service (step 1 of saga): marks slot as Reserved.

    [AllowAnonymous] // Internal service-to-service call; no JWT on internal network
    [HttpPost("{slotId:guid}/reserve")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReserveSlot(Guid slotId)
    {
        var correlationId = Request.Headers.TryGetValue("X-Correlation-Id", out var val)
            ? val.ToString() : "unknown";

        try
        {
            var result = await slotService.ReserveSlotAsync(slotId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message, correlationId }); }
    }

    // ── NEW: POST /api/slots/{slotId}/confirm ────────────────────────────────
    // Called by Booking Service on PaymentSucceeded: marks slot as Occupied.

    [AllowAnonymous]
    [HttpPost("{slotId:guid}/confirm")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmSlot(Guid slotId)
    {
        try
        {
            var result = await slotService.ConfirmSlotAsync(slotId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // ── NEW: POST /api/slots/{slotId}/release ────────────────────────────────
    // Called by Booking Service on PaymentFailed: returns slot to Available.

    [AllowAnonymous]
    [HttpPost("{slotId:guid}/release")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReleaseSlot(Guid slotId)
    {
        try
        {
            var result = await slotService.ReleaseSlotAsync(slotId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}
