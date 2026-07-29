namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Per-line commission eligibility carried across the module boundary (BE-S7). Mirrors the ServiceRequest S1
/// line eligibility so SR can express it over the Payment.Abstraction contract:
/// <list type="bullet">
/// <item><see cref="Eligible"/> → resolve with the <c>CommissionEligibility=Commissionable</c> rule dimension.</item>
/// <item><see cref="Exempt"/> → not commissionable: rate 0, commission 0, provider keeps the full line.</item>
/// <item><see cref="InheritFromCategory"/> → resolve by category/plan/global (no line-eligibility override).</item>
/// </list>
/// Distinct from the Payment rule dimension <c>CommissionEligibility</c> (Commissionable/NonCommissionable).
/// </summary>
public enum LineCommissionEligibility
{
    Eligible            = 1,
    Exempt              = 2,
    InheritFromCategory = 3,
}
