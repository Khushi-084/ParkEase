namespace Payment.Domain.Enums;

/// <summary>
/// FIXED: Added Refunded status to support the case study's refund requirement.
/// </summary>
public enum PaymentStatus
{
    Pending,    // created, awaiting gateway confirmation or cash collection
    Success,    // payment confirmed
    Failed,     // payment attempt failed — a retry is allowed
    Refunded    // FIXED: payment was refunded after cancellation
}