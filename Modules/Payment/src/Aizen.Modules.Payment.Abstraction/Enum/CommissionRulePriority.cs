namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Admin-assigned priority metadata for a CommissionRule.
/// Does NOT affect the resolution precedence engine (ProviderOverride > Plan > Category > Global).
/// Used purely for admin panel display, sorting, and alerting.
/// </summary>
public enum CommissionRulePriority
{
    Low       = 0,
    Standard  = 1,
    Medium    = 2,
    High      = 3,
    EMERGENCY = 4,
}
