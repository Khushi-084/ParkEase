using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.Services;

/// <summary>
/// Orchestrates the booking saga:
///   1. Reserve slot  (HTTP → SlotService)
///   2. Create booking (PENDING)
///   3. Create Razorpay order (HTTP → PaymentService)
///   4. Return order details → client opens Razorpay checkout
///   5. On PaymentSucceeded (via RabbitMQ): confirm slot + mark CONFIRMED
///   6. On PaymentFailed   (via RabbitMQ): release slot  + mark FAILED
/// </summary>
public class BookingService(
    IBookingRepository   bookingRepo,
    ISlotServiceClient   slotClient,
    IPaymentServiceClient paymentClient,
    ILogger<BookingService> logger) : IBookingService
{
    // ── Create booking (saga step 1-3) ────────────────────────────────────────

    public async Task<CreateBookingResponse> CreateBookingAsync(CreateBookingRequest request)
    {
        var correlationId = Guid.NewGuid();
        logger.LogInformation("[Saga:{CorrelationId}] Starting booking for slot {SlotId}",
            correlationId, request.SlotId);

        // Step 1: Reserve the slot
        var reserved = await slotClient.ReserveSlotAsync(request.SlotId, correlationId);
        if (!reserved)
        {
            logger.LogWarning("[Saga:{CorrelationId}] Slot {SlotId} could not be reserved",
                correlationId, request.SlotId);
            throw new InvalidOperationException(
                $"Slot '{request.SlotId}' is not available or could not be reserved.");
        }

        // Step 2: Create PENDING booking
        var booking = new BookingEntity
        {
            SlotId        = request.SlotId,
            UserId        = request.UserId,
            Amount        = request.Amount,
            Status        = BookingStatus.Pending,
            CorrelationId = correlationId
        };
        await bookingRepo.AddAsync(booking);

        logger.LogInformation("[Saga:{CorrelationId}] Booking {BookingId} created (PENDING)",
            correlationId, booking.Id);

        // Step 3: Create Razorpay order via Payment Service
        RazorpayOrderResponse orderResponse;
        try
        {
            orderResponse = await paymentClient.CreateOrderAsync(new CreateRazorpayOrderRequest(
                BookingId:     booking.Id,
                CorrelationId: correlationId,
                Amount:        request.Amount
            ));
        }
        catch (Exception ex)
        {
            // Compensation: release slot and fail the booking
            logger.LogError(ex, "[Saga:{CorrelationId}] Payment order creation failed — rolling back",
                correlationId);

            await slotClient.ReleaseSlotAsync(request.SlotId, correlationId);

            booking.Status    = BookingStatus.Failed;
            booking.UpdatedAt = DateTime.UtcNow;
            await bookingRepo.UpdateAsync(booking);

            throw new InvalidOperationException(
                "Payment order creation failed. Slot has been released. Please try again.");
        }

        // Persist Razorpay order ID
        booking.RazorpayOrderId = orderResponse.OrderId;
        booking.UpdatedAt       = DateTime.UtcNow;
        await bookingRepo.UpdateAsync(booking);

        logger.LogInformation(
            "[Saga:{CorrelationId}] Razorpay order {OrderId} created — waiting for payment result",
            correlationId, orderResponse.OrderId);

        return new CreateBookingResponse(
            BookingId:       booking.Id,
            SlotId:          booking.SlotId,
            UserId:          booking.UserId,
            Amount:          booking.Amount,
            Status:          booking.Status.ToString(),
            RazorpayOrderId: orderResponse.OrderId,
            RazorpayKeyId:   orderResponse.KeyId,
            Currency:        orderResponse.Currency,
            CorrelationId:   correlationId,
            CreatedAt:       booking.CreatedAt
        );
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<BookingResponse> GetByIdAsync(Guid bookingId)
    {
        var booking = await bookingRepo.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking '{bookingId}' not found.");
        return Map(booking);
    }

    public async Task<IEnumerable<BookingResponse>> GetByUserIdAsync(Guid userId)
    {
        var bookings = await bookingRepo.GetByUserIdAsync(userId);
        return bookings.Select(Map);
    }

    // ── Saga compensation / confirmation (called by RabbitMQ consumer) ────────

    public async Task ConfirmBookingAsync(Guid correlationId, string razorpayPaymentId)
    {
        logger.LogInformation(
            "[Saga:{CorrelationId}] PaymentSucceeded — confirming booking", correlationId);

        var booking = await bookingRepo.GetByCorrelationIdAsync(correlationId)
            ?? throw new KeyNotFoundException(
                $"Booking for correlationId '{correlationId}' not found.");

        if (booking.Status != BookingStatus.Pending)
        {
            logger.LogWarning(
                "[Saga:{CorrelationId}] Booking already in status {Status} — skipping confirm",
                correlationId, booking.Status);
            return;
        }

        // Confirm slot in SlotService
        await slotClient.ConfirmSlotAsync(booking.SlotId, correlationId);

        booking.Status    = BookingStatus.Confirmed;
        booking.UpdatedAt = DateTime.UtcNow;
        await bookingRepo.UpdateAsync(booking);

        logger.LogInformation(
            "[Saga:{CorrelationId}] Booking {BookingId} CONFIRMED", correlationId, booking.Id);
    }

    public async Task FailBookingAsync(Guid correlationId, string reason)
    {
        logger.LogInformation(
            "[Saga:{CorrelationId}] PaymentFailed ({Reason}) — releasing slot", correlationId, reason);

        var booking = await bookingRepo.GetByCorrelationIdAsync(correlationId)
            ?? throw new KeyNotFoundException(
                $"Booking for correlationId '{correlationId}' not found.");

        if (booking.Status != BookingStatus.Pending)
        {
            logger.LogWarning(
                "[Saga:{CorrelationId}] Booking already in status {Status} — skipping fail",
                correlationId, booking.Status);
            return;
        }

        // Compensation: release slot
        await slotClient.ReleaseSlotAsync(booking.SlotId, correlationId);

        booking.Status    = BookingStatus.Failed;
        booking.UpdatedAt = DateTime.UtcNow;
        await bookingRepo.UpdateAsync(booking);

        logger.LogInformation(
            "[Saga:{CorrelationId}] Booking {BookingId} FAILED — slot released", correlationId, booking.Id);
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static BookingResponse Map(Domain.Entities.BookingEntity b) => new(
        Id:              b.Id,
        SlotId:          b.SlotId,
        UserId:          b.UserId,
        Amount:          b.Amount,
        Status:          b.Status.ToString(),
        RazorpayOrderId: b.RazorpayOrderId,
        CorrelationId:   b.CorrelationId,
        CreatedAt:       b.CreatedAt,
        UpdatedAt:       b.UpdatedAt
    );
}