namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>Lifecycle of a provider commission-benefit entitlement (BE-P7). Stable int (HasConversion&lt;int&gt;).</summary>
public enum ProviderCommissionBenefitEntitlementStatus
{
    Active    = 1,
    Exhausted = 2,   // usage limit and/or eligible GMV fully consumed
    Expired   = 3,   // past GrantedTo
    Revoked   = 4,   // admin-revoked
}
