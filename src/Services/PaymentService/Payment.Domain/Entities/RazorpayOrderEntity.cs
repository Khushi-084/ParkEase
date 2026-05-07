using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Payment.Domain.Entities;

/// <summary>
/// Tracks Razorpay orders created by this service.
/// Used for idempotency — a webhook for the same order is only processed once.
/// </summary>
[Table("RazorpayOrders")]
public class RazorpayOrderEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Booking ID from Booking Service (cross-service reference).</summary>
    [Required]
    public Guid BookingId { get; set; }

    /// <summary>Correlation ID for saga tracking.</summary>
    [Required]
    public Guid CorrelationId { get; set; }

    /// <summary>Razorpay order ID (e.g. order_xxxx).</summary>
    [Required]
    [MaxLength(100)]
    public string RazorpayOrderId { get; set; } = string.Empty;

    /// <summary>Amount in paise.</summary>
    [Required]
    public int AmountInPaise { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "INR";

    /// <summary>created, attempted, paid, failed</summary>
    [MaxLength(20)]
    public string Status { get; set; } = "created";

    /// <summary>Set once the webhook is processed — prevents duplicate handling.</summary>
    public bool WebhookProcessed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}