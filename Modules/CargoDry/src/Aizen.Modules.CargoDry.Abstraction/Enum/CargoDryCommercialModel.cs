namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// CargoDry-side copy of the Payment module's CommercialModel enum.
/// Integer values are intentionally identical to Payment.Abstraction.Enum.CommercialModel
/// so that cross-module mapping is a simple cast.
///
/// Used in CargoDryKitEntity and CargoDryBatchEntity to describe the commercial arrangement
/// without creating a hard dependency on Payment.Abstraction from CargoDry.Abstraction.
/// </summary>
public enum CargoDryCommercialModel
{
    /// <summary>Platform intermediary. Provider receives payout after commission deduction.</summary>
    MarketplaceCommission = 1,

    /// <summary>Inktavia sells directly; no provider commission split.</summary>
    PrincipalSale         = 2,

    /// <summary>Recurring subscription billing. Not used in kit/batch context directly.</summary>
    SubscriptionBilling   = 3,
}
