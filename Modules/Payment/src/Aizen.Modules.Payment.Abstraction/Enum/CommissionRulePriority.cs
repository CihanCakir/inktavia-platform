namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Admin-assigned priority for a CommissionRule.
/// BE-P2 (§13.7): acts as the <b>secondary</b> selector in resolution — after SpecificityRank, the highest
/// Priority wins; if two active rules still tie on (SpecificityRank, Priority) the resolver fails loud with
/// CommissionRuleConflict rather than guessing. Also used for admin panel display, sorting, and alerting.
/// </summary>
public enum CommissionRulePriority
{
    Low       = 0,
    Standard  = 1,
    Medium    = 2,
    High      = 3,
    EMERGENCY = 4,
}
