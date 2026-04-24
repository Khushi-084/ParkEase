using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookingService.Domain.Enums;

namespace BookingService.Domain.Entities;

[Table("Bookings")]
public class BookingEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SlotId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [Column("Amount", TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>Razorpay order ID returned by Payment Service.</summary>
    [MaxLength(100)]
    public string? RazorpayOrderId { get; set; }

    /// <summary>Correlation ID propagated across the saga.</summary>
    [Required]
    public Guid CorrelationId { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}