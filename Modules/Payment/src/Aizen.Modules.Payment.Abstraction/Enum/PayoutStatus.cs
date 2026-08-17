namespace Aizen.Modules.Payment.Abstraction.Enum;

public enum PayoutStatus
{
    Pending    = 1,
    Processing = 2,
    Completed  = 3,
    Failed     = 4,
    Cancelled  = 5,
    OnHold     = 6,

    /// <summary>
    /// Admin has explicitly approved the payout for disbursement.
    /// Transition: Pending → Approved (ApproveCargoDrySettlementPayoutCommand).
    /// Phase 4D (July 2026): CargoDry sell-through settlement payout lifecycle.
    /// </summary>
    Approved   = 7,
}
