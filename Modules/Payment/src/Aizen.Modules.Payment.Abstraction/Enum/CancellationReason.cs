namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Why a PendingIntent transaction was cancelled before capture.
/// Used by PaymentTransactionEntity.Cancel(reason) and audit trails.
/// </summary>
public enum CancellationReason
{
    /// <summary>Payer explicitly requested cancellation.</summary>
    PayerRequest        = 1,

    /// <summary>Provider requested order cancellation.</summary>
    ProviderRequest     = 2,

    /// <summary>EscrowTimeoutJob: payment not completed within timeout window.</summary>
    SystemTimeout       = 3,

    /// <summary>Admin forced the cancellation via admin panel.</summary>
    AdminForced         = 4,

    /// <summary>StaleEscrowCleanupJob: transaction abandoned for configured days.</summary>
    AbandonedIntent     = 5,

    /// <summary>Fraud or security risk detected.</summary>
    FraudSuspicion      = 6,

    /// <summary>Linked service request was cancelled by one of the parties.</summary>
    ServiceRequestCancelled = 7,

    /// <summary>Duplicate transaction detected; one was cancelled.</summary>
    DuplicateDetected   = 8,
}
