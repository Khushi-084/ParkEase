using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.API.Controllers;

[Authorize]
[ApiController]
[Route("api/payment")]
[Produces("application/json")]
public class PaymentController(IPaymentService paymentService) : ControllerBase
{
    // ── GET /api/payment/{paymentId} ──────────────────────────────────────────

    [HttpGet("{paymentId:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentById(Guid paymentId)
    {
        try   { return Ok(await paymentService.GetByIdAsync(paymentId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── GET /api/payment/booking/{bookingId} ──────────────────────────────────

    /// <summary>
    /// Retrieve the payment for a pre-booking (BookingService flow).
    /// </summary>
    [HttpGet("booking/{bookingId:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentByBooking(Guid bookingId)
    {
        try   { return Ok(await paymentService.GetByBookingIdAsync(bookingId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── GET /api/payment/ticket/{ticketId} ────────────────────────────────────

    /// <summary>
    /// Retrieve the payment for a walk-in ticket (TicketService flow).
    /// </summary>
    [HttpGet("ticket/{ticketId:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentByTicket(Guid ticketId)
    {
        try   { return Ok(await paymentService.GetByTicketIdAsync(ticketId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── POST /api/payment ─────────────────────────────────────────────────────

    /// <summary>
    /// Create a payment. Supports two flows:
    ///   • Pre-booking  → supply bookingId, omit ticketId
    ///   • Walk-in exit → supply ticketId,  omit bookingId
    ///
    /// For online modes (Card/UPI/Wallet): creates a Razorpay order and
    /// returns razorpayOrderId for the frontend to open checkout.
    /// For Cash: immediately marks payment as Success.
    /// Returns 409 if a non-failed payment already exists.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        try
        {
            var payment = await paymentService.CreatePaymentAsync(request);
            return CreatedAtAction(
                nameof(GetPaymentById),
                new { paymentId = payment.PaymentId },
                payment);
        }
        catch (ArgumentException ex)         { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    // ── PATCH /api/payment/{paymentId}/status ─────────────────────────────────

    [HttpPatch("{paymentId:guid}/status")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePaymentStatus(
        Guid paymentId, [FromBody] UpdatePaymentStatusRequest request)
    {
        if (paymentId == Guid.Empty)
            return BadRequest(new { error = "paymentId must be a valid non-empty GUID." });

        try
        {
            return Ok(await paymentService.UpdatePaymentStatusAsync(paymentId, request));
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (ArgumentException ex)         { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── POST /api/payment/{paymentId}/refund ──────────────────────────────────

    /// <summary>
    /// Refund a successful payment.
    /// Sets status to Refunded and records RefundedAt timestamp.
    /// Only Success payments can be refunded.
    /// </summary>
    [HttpPost("{paymentId:guid}/refund")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object),          StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefundPayment(
        Guid paymentId, [FromBody] RefundPaymentRequest request)
    {
        if (paymentId == Guid.Empty)
            return BadRequest(new { error = "paymentId must be a valid non-empty GUID." });

        try
        {
            return Ok(await paymentService.RefundAsync(paymentId, request));
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}