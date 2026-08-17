using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.ProfitProtection;

/// <summary>
/// BE-S9 (§20.12) — one already-priced offer line handed to <see cref="LineProfitProtectionEngine.Evaluate"/>. Carries only
/// the post-allocation line economics the line-level gate needs; the engine performs NO cost derivation (part cost never
/// enters Payment's outward surface — S5 already resolved the cost-free caps). <see cref="IsPartLine"/> selects the floor
/// source: part lines (Product/Consumable) take their floors from the S5 <c>PartLineAllowanceDto</c>, every other line from
/// the versioned policy defaults.
/// </summary>
public sealed record LineProfitProtectionLineInput(
    string  LineRef,
    bool    IsPartLine,
    decimal ProviderNet,                 // reducedGross − commission for this line (§19.9)
    decimal ProviderFundedDiscount,      // provider-funded portion of the customer discount on this line
    decimal PlatformFundedDiscount,      // platform-funded portion of the customer discount on this line
    decimal ResolvedRate,                // the S7 base commission rate (for the P7 commission-floor contract)
    decimal CommissionNetRevenue,        // the commission the platform earns on this line
    decimal CommissionBase,              // pro-rata weight for the transaction platform fee / benefit attribution
    decimal LineBase);                   // line gross-before-discount — the base for the non-part rate floors

/// <summary>
/// BE-S9 — the cost-free line floor set the engine applies to a PART line. Mapped at the application boundary from the S5
/// <see cref="Aizen.Modules.Payment.Abstraction.RemoteCall.Responses.PartLineAllowanceDto"/> so the domain engine never
/// depends on the abstraction DTO. <see cref="Found"/> false ⇒ no term resolved ⇒ ConfigurationError for that part line.
/// </summary>
public sealed record LinePartAllowance(
    bool    Found,
    decimal MinimumProviderReceivable,
    decimal AllowedProviderFundedDiscount,
    decimal AllowedPlatformFundedDiscount);

/// <summary>The distinct line-level breach that failed a line (or None). Maps to a distinct surfaced error code.</summary>
public enum LineProfitProtectionBreach
{
    None                        = 0,
    ProviderReceivableBelowFloor = 1,   // ProviderNet < min-receivable
    FundedDiscountExceedsCap     = 2,   // provider- or platform-funded discount over the allowed cap
    CommissionBelowFloor         = 3,   // ResolvedRate < commission floor (reuses the P7 contract)
    NegativeContribution         = 4,   // line platform contribution below the min, no in-limit strategic-loss exception
    MissingAllowance             = 5,   // a part line with no resolved S5 allowance → ConfigurationError
}

/// <summary>
/// BE-S9 — per-line verdict (descriptive; the numbers are unchanged). <see cref="Passed"/> is false on a real breach;
/// <see cref="StrategicLossExceptionApplied"/> marks a line that ran below its contribution floor but within the versioned
/// strategic-loss limit (allowed + logged, never silent). <see cref="ErrorCode"/> is the distinct surfaced code on failure.
/// </summary>
public sealed record LineProfitProtectionResult(
    string  LineRef,
    bool    Passed,
    LineProfitProtectionBreach Breach,
    decimal ProviderMinimumReceivableApplied,
    decimal LinePlatformContribution,
    decimal MinLinePlatformContribution,
    bool    StrategicLossExceptionApplied,
    int?    ErrorCode,
    string? Reason);

/// <summary>
/// BE-S9 — the overall line-level evaluation. Pure output: <see cref="Passed"/> = every line cleared its own floor (NO
/// netting — a positive line never offsets a failing one). <see cref="State"/> is Approved / Rejected / ConfigurationError,
/// consumed by the combiner to short-circuit BEFORE the §19.2 transaction gates.
/// </summary>
public sealed record LineProfitProtectionEvaluation(
    bool                                     Passed,
    ProfitProtectionDecisionState            State,
    IReadOnlyList<LineProfitProtectionResult> Lines,
    int?                                     PrimaryErrorCode,
    string?                                  Reason)
{
    public bool IsConfigurationError => State == ProfitProtectionDecisionState.ConfigurationError;
}
