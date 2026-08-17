namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// Lifecycle status of a CargoDry renewal preparation workflow.
/// Progresses from Draft through to Completed or Cancelled.
/// Notification dispatch is an optional step that does not gate invoice or completion.
/// </summary>
public enum CargoDryRenewalPreparationStatus
{
    /// <summary>Preparation record created; not yet ready for invoice or notification.</summary>
    Draft = 0,

    /// <summary>Preparation validated and ready for invoice/notification steps.</summary>
    Prepared = 1,

    /// <summary>Draft invoice has been created in the Payment module.</summary>
    InvoicePrepared = 2,

    /// <summary>Notification preview/intent has been created; not yet dispatched.</summary>
    NotificationPrepared = 3,

    /// <summary>Notification has been published to the message bus for delivery.</summary>
    NotificationQueued = 4,

    /// <summary>Notification consumer confirmed dispatch (message sent to provider channel).</summary>
    NotificationDispatched = 5,

    /// <summary>
    /// Renewal has been confirmed (payment reference present or manual confirmed).
    /// Kit has been renewed via RenewKitCommand. Lifecycle event written.
    /// Terminal state.
    /// </summary>
    Completed = 6,

    /// <summary>
    /// Preparation was cancelled before completion.
    /// Kit was NOT renewed. Invoice may still exist independently.
    /// Terminal state.
    /// </summary>
    Cancelled = 7,

    /// <summary>
    /// Preparation failed due to an irrecoverable error (e.g., payment failure during completion).
    /// Terminal state.
    /// </summary>
    Failed = 8,
}
