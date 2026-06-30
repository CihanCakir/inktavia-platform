namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Lifecycle status of an individual TransactionRefundRecord.
/// </summary>
public enum TransactionRefundStatus
{
    /// <summary>Refund created but not yet confirmed by the gateway (async refund flow).</summary>
    Pending   = 1,

    /// <summary>Refund successfully processed by the gateway and money returned to payer.</summary>
    Processed = 2,

    /// <summary>
    /// This refund was reversed (cancelled). The refunded amount was re-deducted from
    /// TotalRefundedAmount on the parent transaction.
    /// Reversals are only possible before the payer's bank settles the refund.
    /// Post-MVP: link to a new payment transaction for the re-charge.
    /// </summary>
    Reversed  = 3,

    /// <summary>Gateway rejected the refund. See FailureReason on the record.</summary>
    Failed    = 4,
}
