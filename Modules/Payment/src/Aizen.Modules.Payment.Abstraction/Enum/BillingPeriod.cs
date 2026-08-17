namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Billing cadence a plan price applies to (BE-P4). Integer values are stable (HasConversion&lt;int&gt;).
/// </summary>
public enum BillingPeriod
{
    Monthly = 1,
    Annual  = 2,
}
