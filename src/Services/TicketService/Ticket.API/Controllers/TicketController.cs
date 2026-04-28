using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticket.Application.DTOs;
using Ticket.Application.Interfaces;

namespace Ticket.API.Controllers;

/// <summary>
/// Manages the full parking ticket lifecycle:
/// vehicle entry (ticket creation) and vehicle exit (billing + slot release + payment).
/// </summary>
[ApiController]
[Route("api/v1/ticket")]
[Produces("application/json")]
public class TicketController(ITicketService ticketService) : ControllerBase
{
    // ── GET /api/v1/ticket/{ticketId} ────────────────────────────────────────

    [AllowAnonymous]
    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketById(Guid ticketId)
    {
        try
        {
            return Ok(await ticketService.GetByIdAsync(ticketId));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── GET /api/v1/ticket/lot/{lotId}/active-count ──────────────────────────

    /// <summary>
    /// Returns the count of Active tickets for a lot.
    /// Called by ParkingLotService before deleting a lot.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("lot/{lotId:guid}/active-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveCountByLot(Guid lotId)
    {
        var count = await ticketService.GetActiveCountByLotAsync(lotId);
        return Ok(new { count });
    }

    // ── POST /api/v1/ticket ──────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        try
        {
            var ticket = await ticketService.CreateTicketAsync(request);
            return CreatedAtAction(nameof(GetTicketById), new { ticketId = ticket.Id }, ticket);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error  = "SlotService is currently unreachable. Please try again later.",
                detail = ex.Message
            });
        }
    }

    // ── PUT /api/v1/ticket/exit/{ticketId} ────────────────────────────────────

    /// <summary>
    /// Process vehicle exit.
    /// Flow: calculate billing → release slot → complete ticket → create payment.
    /// </summary>
    [AllowAnonymous]
    [HttpPut("exit/{ticketId}")]
    [ProducesResponseType(typeof(ExitTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object),              StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object),              StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object),              StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ExitTicket(
        string ticketId,
        [FromBody] ExitTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(ticketId))
            return BadRequest(new { error = "ticketId must be provided." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await ticketService.ExitTicketAsync(ticketId, request.PaymentMode.ToString());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error  = "A downstream service is currently unreachable. Please try again later.",
                detail = ex.Message
            });
        }
    }
}
