using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Payment.Application.DTOs;

public record VerifyRazorpayPaymentRequest(
    [Required] [property: JsonPropertyName("razorpayOrderId")]   string RazorpayOrderId,
    [Required] [property: JsonPropertyName("razorpayPaymentId")] string RazorpayPaymentId,
    [Required] [property: JsonPropertyName("razorpaySignature")] string RazorpaySignature
);
