namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Lifecycle status of a CommissionRule.
/// Derived automatically from EffectiveFrom / EffectiveTo / IsActive,
/// or set explicitly to Inactive via DeactivateCommissionRuleCommand.
/// </summary>
public enum CommissionRuleStatus
{
    Draft     = 0,  // Created but not yet effective / pending review
    Active    = 1,  // Currently effective (EffectiveFrom ≤ now AND (EffectiveTo == null OR EffectiveTo ≥ now))
    Scheduled = 2,  // EffectiveFrom is in the future
    Expired   = 3,  // EffectiveTo is in the past
    Inactive  = 4,  // Manually deactivated by admin via DeactivateCommissionRuleCommand
}
