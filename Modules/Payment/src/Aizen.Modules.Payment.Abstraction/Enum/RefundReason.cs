namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Why a refund (full or partial) was issued.
/// Used on TransactionRefundRecord and surfaced in admin panel + Notification module.
///
/// Migration note: existing data used namespace Aizen.Modules.Payment.Abstraction.
/// All new code must use the Enum sub-namespace.
/// </summary>
public enum RefundReason
{
    // ── Payer-initiated ───────────────────────────────────────────────────────

    /// <summary>Payer voluntarily cancelled before or after service delivery.</summary>
    UserCancel              = 1,

    /// <summary>Payer reported non-delivery or service failure.</summary>
    ServiceNotDelivered     = 2,

    /// <summary>Payer and provider reached mutual cancellation agreement.</summary>
    MutualAgreement         = 3,

    // ── Provider-initiated ────────────────────────────────────────────────────

    /// <summary>Provider cancelled the service (e.g., unavailability).</summary>
    OrganizerCancel         = 4,

    /// <summary>Provider failed to deliver the agreed service.</summary>
    ProviderFailedToDeliver = 5,

    // ── System / Platform ─────────────────────────────────────────────────────

    /// <summary>Payment gateway or internal system error required a refund.</summary>
    SystemError             = 6,

    /// <summary>Linked ServiceRequest was cancelled — auto-refund triggered by consumer.</summary>
    ServiceRequestCancelled = 7,

    /// <summary>Duplicate charge detected; one transaction refunded.</summary>
    DuplicateCharge         = 8,

    // ── Dispute / Admin ───────────────────────────────────────────────────────

    /// <summary>Dispute resolved in payer's favour.</summary>
    DisputeResolvedForPayer = 9,

    /// <summary>Admin issued a manual refund (e.g., goodwill, error correction).</summary>
    AdminForced             = 10,

    /// <summary>Fraud investigation completed; funds returned to payer.</summary>
    FraudConfirmed          = 11,

    // ── Partial-specific ──────────────────────────────────────────────────────

    /// <summary>Partial service delivered; prorated refund issued.</summary>
    PartialServiceDelivered = 12,

    /// <summary>Promotional adjustment or price correction — partial refund only.</summary>
    PriceAdjustment         = 13,

    /// <summary>Compensation for delay or inconvenience — partial refund only.</summary>
    CompensationCredit      = 14,
}
