namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Whether a refund record covers the full original amount or a portion.
/// </summary>
public enum RefundType
{
    /// <summary>Full gross amount returned to payer. Transaction moves to Refunded.</summary>
    Full    = 1,

    /// <summary>
    /// Partial amount returned. Transaction moves to PartiallyRefunded.
    /// Multiple partial refunds can be issued until RemainingRefundableAmount reaches zero.
    /// </summary>
    Partial = 2,
}
