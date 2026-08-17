namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Lifecycle status for a PaymentTransaction.
/// Replaces the legacy PaymentStatus — use this for all new code.
/// </summary>
public enum PaymentTransactionStatus
{
    PendingIntent      = 1,  // Intent created, money not yet collected
    Captured           = 2,  // Money collected; held in escrow (EscrowRequired=true) or settled immediately
    Released           = 3,  // Escrow released to provider after job completion
    Refunded           = 4,  // Full refund issued to payer
    PartiallyRefunded  = 5,  // Partial refund issued
    Failed             = 6,  // Gateway rejected the payment
    Disputed           = 7,  // Under dispute — funds frozen
    Cancelled          = 8,  // Intent cancelled before capture
}
