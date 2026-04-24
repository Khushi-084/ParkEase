using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

/// <summary>
/// Represents a payment record.
/// 
/// Supports two flows:
///   • Pre-booking flow  → BookingId is set, TicketId is null
///   • Walk-in flow      → TicketId is set, BookingId is null
/// Exactly one of BookingId / TicketId will be non-null at any time.
/// </summary>
[Table("Payments")]
public class PaymentEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid PaymentId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Set for pre-booking flow (BookingService → PaymentService).
    /// Null for walk-in (TicketService) flow.
    /// </summary>
    [Column("BookingId")]
    public Guid? BookingId { get; set; }

    /// <summary>
    /// Set for walk-in flow (TicketService → PaymentService after exit).
    /// Null for pre-booking flow.
    /// </summary>
    [Column("TicketId")]
    public Guid? TicketId { get; set; }

    /// <summary>Amount paid in INR. Must be greater than zero.</summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    [Column("Amount", TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    /// <summary>Payment mode — Card, UPI, Wallet, or Cash.</summary>
    [Required]
    [Column("Mode", TypeName = "text")]
    public PaymentMode? Mode { get; set; } 

    /// <summary>
    /// Reference ID from the payment gateway.
    /// Null for Cash — a system-generated receipt is used instead.
    /// </summary>
    [MaxLength(100)]
    [Column("TransactionId")]
    public string? TransactionId { get; set; }

    /// <summary>Current status: Pending, Success, Failed, or Refunded.</summary>
    [Required]
    [Column("Status", TypeName = "text")]
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>UTC timestamp when this payment record was created.</summary>
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when this payment was refunded. Null until refunded.</summary>
    [Column("RefundedAt")]
    public DateTime? RefundedAt { get; set; }
}