namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Which advantage the engine reduces first when the requested combination is unsafe (§19.11 AdjustmentOrder).
/// Policy-configurable. Integer values are stable (HasConversion&lt;int&gt;).
/// </summary>
public enum ProfitProtectionAdjustmentOrder
{
    /// <summary>Reduce the platform-funded customer discount first, then the provider commission benefit.</summary>
    PlatformDiscountThenCommissionBenefit = 1,

    /// <summary>Reduce the provider commission benefit first, then the platform-funded customer discount.</summary>
    CommissionBenefitThenPlatformDiscount = 2,
}
