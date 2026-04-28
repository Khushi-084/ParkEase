using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/bookings")]
[Produces("application/json")]
public class BookingController(IBookingService bookingService) : ControllerBase
{
    /// <summary>
    /// Create an on-the-spot booking.
    /// Flow: reserve slot → create PENDING booking → create Razorpay order
    /// → return order details so client can open checkout widget.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateBookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await bookingService.CreateBookingAsync(request);
            return CreatedAtAction(nameof(GetBooking), new { bookingId = result.BookingId }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex)         { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Get booking by ID.</summary>
    [HttpGet("{bookingId:guid}")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBooking(Guid bookingId)
    {
        try   { return Ok(await bookingService.GetByIdAsync(bookingId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    /// <summary>Get all bookings for a user.</summary>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<BookingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserBookings(Guid userId)
        => Ok(await bookingService.GetByUserIdAsync(userId));

    /// <summary>
    /// Internal endpoint to confirm a booking directly. Used as a fallback if RabbitMQ is down.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("internal/confirm/{correlationId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InternalConfirmBooking(Guid correlationId)
    {
        try
        {
            await bookingService.ConfirmBookingAsync(correlationId, "direct-fallback");
            return Ok(new { message = "Booking confirmed via internal fallback." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

