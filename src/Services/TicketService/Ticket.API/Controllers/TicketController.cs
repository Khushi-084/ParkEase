using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Ticket.Application.DTOs;
using Ticket.Application.Interfaces;

namespace Ticket.API.Controllers;

/// <summary>
/// Manages the full parking ticket lifecycle:
/// vehicle entry (ticket creation) and vehicle exit (billing + slot release).
/// </summary>
[ApiController]
[Route("api/ticket")]
[Produces("application/json")]
public partial class TicketController(ITicketService ticketService) : ControllerBase
{
    // Compiled regex for Indian vehicle number validation at controller level.
    // Acts as a first-pass guard before the request even reaches the service layer.
    [GeneratedRegex(@"^[A-Z]{2}[0-9]{1,2}[A-Z]{1,3}[0-9]{4}$", RegexOptions.Compiled)]
    private static partial Regex VehicleNumberRegex();

    // ── POST /api/ticket ─────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new parking ticket for a vehicle entering the lot.
    /// Allocates an available slot and records entry time.
    /// </summary>
    /// <param name="request">Vehicle number in Indian registration format e.g. MH12AB1234.</param>
    /// <returns>Created ticket with slot details and entry time.</returns>
    /// <response code="201">Ticket created successfully.</response>
    /// <response code="400">Invalid vehicle number format or no slots available.</response>
    /// <response code="503">SlotService is unreachable.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TicketResponse),  StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object),           StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object),           StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest request)
    {
        // Normalise to uppercase before any processing
        var normalised = request.VehicleNumber.Trim().ToUpperInvariant();

        // Controller-level regex guard (second safety net after DTO annotation)
        if (!VehicleNumberRegex().IsMatch(normalised))
        {
            return BadRequest(new
            {
                error  = "Invalid vehicle number format.",
                detail = "Expected Indian registration format e.g. MH12AB1234 " +
                         "(2 letters + 1-2 digits + 1-3 letters + 4 digits)."
            });
        }

        // Rebuild request with the normalised number
        var normalisedRequest = request with { VehicleNumber = normalised };

        try
        {
            var ticket = await ticketService.CreateTicketAsync(normalisedRequest);
            return CreatedAtAction(nameof(ExitTicket), new { ticketId = ticket.Id }, ticket);
        }
        catch (InvalidOperationException ex)
        {
            // e.g. no slots available
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

    // ── PUT /api/ticket/exit/{ticketId} ──────────────────────────────────────

    /// <summary>
    /// Processes a vehicle exit: calculates the parking bill, releases the slot,
    /// and marks the ticket as Completed.
    /// Pricing rule: Rs.20 per hour, rounded up to the next whole hour.
    /// </summary>
    /// <param name="ticketId">GUID of the active ticket to close.</param>
    /// <returns>Completed ticket with duration and amount charged.</returns>
    /// <response code="200">Exit processed and bill calculated.</response>
    /// <response code="400">Ticket is already completed.</response>
    /// <response code="404">Ticket not found.</response>
    /// <response code="503">SlotService is unreachable.</response>
    [HttpPut("exit/{ticketId:guid}")]
    [ProducesResponseType(typeof(ExitTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object),              StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object),              StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object),              StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ExitTicket(Guid ticketId)
    {
        // Guard: reject obviously invalid (empty) GUID before hitting the DB
        if (ticketId == Guid.Empty)
        {
            return BadRequest(new { error = "ticketId must be a valid non-empty GUID." });
        }

        try
        {
            var result = await ticketService.ExitTicketAsync(ticketId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // e.g. ticket already completed
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
}