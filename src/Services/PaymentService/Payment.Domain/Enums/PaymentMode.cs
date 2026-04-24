namespace Payment.Domain.Enums;

/// <summary>
/// FIXED: New enum — payment mode was completely missing from the service.
/// The case study requires: Card, UPI, Wallet, Cash (pay on exit).
/// </summary>
public enum PaymentMode
{
    Cash = 0,   // make Cash the CLR default (= 0)
    Card = 1,
    UPI  = 2,
    Wallet = 3     // offline — paid physically at the exit gate (COD)
}