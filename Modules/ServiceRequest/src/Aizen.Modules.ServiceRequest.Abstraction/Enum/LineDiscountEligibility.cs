namespace Aizen.Modules.ServiceRequest.Abstraction.Enum;

/// <summary>
/// BE-S6 — per-line eligibility for a <b>customer</b> (platform/plan) discount (§20.10). Distinct from
/// <see cref="LineCommissionEligibility"/>. Default by ItemType economic role (Service/Labor/Product → Eligible;
/// Travel/marina/pass-through → Exempt). <b>Exempt lines receive no customer discount.</b> Admin-tunable + per-line override.
/// </summary>
public enum LineDiscountEligibility
{
    Eligible            = 1,
    Exempt              = 2,
    InheritFromCategory = 3,
}
