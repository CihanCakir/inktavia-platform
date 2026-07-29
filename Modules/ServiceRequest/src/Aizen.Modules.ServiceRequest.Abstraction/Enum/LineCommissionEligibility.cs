namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

/// <summary>
/// Per-line commission eligibility DIMENSION (§20.11). S1 stores the dimension only — the commission RATE/amount is
/// resolved in S7 (fed into the Payment <c>CommissionRule</c> dimensions). The default per line is derived from the
/// item's economic role (see <c>ServiceRequestOfferItemEntity.DefaultEligibilityForItemType</c>) but is admin/
/// category-tunable and overridable per line — not a baked business constant.
/// </summary>
public enum LineCommissionEligibility
{
    /// <summary>Commissionable: the line's net contributes to the commission base.</summary>
    Eligible            = 1,

    /// <summary>Pass-through / non-commissionable: contributes 0 to the commission base.</summary>
    Exempt              = 2,

    /// <summary>Base contributes like Eligible, but the RATE decision defers to S7's category resolution.</summary>
    InheritFromCategory = 3,
}
