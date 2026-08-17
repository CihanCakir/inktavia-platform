namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// BE-P10 §7.1 — the economic cause of a refund. Distinct from <see cref="RefundReason"/> (the operational reason on the
/// refund record); <c>RefundCauseMap</c> reconciles the two. Drives the <see cref="RefundAllocationPolicyEntity"/> lookup.
/// </summary>
public enum RefundCause
{
    ProviderCancelled                 = 1,
    CustomerCancelledBeforeWork       = 2,
    CustomerCancelledAfterWorkStarted = 3,
    DisputeCustomerFavoured           = 4,
    DisputeProviderFavoured           = 5,
    TechnicalFailure                  = 6,
    DuplicatePayment                  = 7,
    AdministrativeCorrection          = 8,
}

/// <summary>BE-P10 §7.2/§7.3 — whether the provider had already been released/settled when the refund happens.</summary>
public enum ReleaseState
{
    /// <summary>Provider not yet released (funds still in the protected pool) — cancel the reversal, no clawback.</summary>
    BeforeProviderRelease = 1,
    /// <summary>Provider already paid — recover via the §7.3 order (balance → offset → negative balance).</summary>
    AfterProviderRelease  = 2,
}

/// <summary>BE-P10 §7.1 — how the platform fee is refunded for a given cause.</summary>
public enum PlatformFeeRefundMode
{
    Full        = 1,   // refund the whole snapshot platform fee
    ProRata     = 2,   // refund proportional to the service ratio
    None        = 3,   // keep the platform fee
    FixedAmount = 4,   // refund a fixed policy amount
    RuleBased   = 5,   // per policy (MVP: treated as ProRata)
}

/// <summary>BE-P10 §7.4 — a provider negative-balance ledger movement.</summary>
public enum ProviderBalanceMovementType
{
    RefundClawback      = 1,   // provider owes back the refunded net (balance ↓, toward negative)
    ChargebackClawback  = 2,
    PayoutOffset        = 3,   // a payout closes the negative balance first (balance ↑)
    ManualAdjustment    = 4,   // admin correction (audited)
}
