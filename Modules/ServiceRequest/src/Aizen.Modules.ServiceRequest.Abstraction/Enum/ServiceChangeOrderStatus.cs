namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

/// <summary>
/// BE-S11b — lifecycle of a post-acceptance change order (§20.13). A change order carries proposed extra/removed work and
/// only affects money once the <b>customer</b> approves and it is applied (a new economics snapshot + a new escrow/split, or
/// a P10 refund for a reduction — the accepted snapshot is never mutated).
/// </summary>
public enum ServiceChangeOrderStatus
{
    /// <summary>Provider proposed it. Nothing financial has happened — not in any total or collection.</summary>
    Proposed         = 1,
    /// <summary>Customer approved it (transient); the apply step runs immediately after.</summary>
    CustomerApproved = 2,
    /// <summary>Rejected — terminal. Either the customer rejected, or the incremental economics breached P5/S9 (no snapshot/collection).</summary>
    Rejected         = 3,
    /// <summary>Applied — the incremental snapshot + escrow (increase) or the P10 refund (decrease) is in place. Terminal.</summary>
    Applied          = 4,
    /// <summary>Provider withdrew the proposal before approval. Terminal.</summary>
    Cancelled        = 5,
}
