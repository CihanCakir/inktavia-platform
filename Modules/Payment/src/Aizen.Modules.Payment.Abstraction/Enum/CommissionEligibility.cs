namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Whether a line is subject to commission (§20.11). A commission rule may be scoped to a specific
/// eligibility so, e.g., a Non-commissionable pass-through line resolves to a different (or zero) rule
/// than a Commissionable labour line. Null on a rule = applies to any eligibility.
/// </summary>
public enum CommissionEligibility
{
    Commissionable    = 1,
    NonCommissionable = 2,
}
