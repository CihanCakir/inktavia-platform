namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Describes the commercial structure under which an invoice is generated.
/// Drives who is the legal seller, who takes the margin, and how VAT/KDV flows.
///
/// MVP: Only MarketplaceCommission and SubscriptionBilling are active.
/// PrincipalSale and ManagedService are reserved for future platform product sales.
/// </summary>
public enum CommercialModel
{
    /// <summary>
    /// Platform acts as marketplace intermediary.
    /// Inktavia collects payment on behalf of the provider, then disburses net payout
    /// after deducting commission. Both a buyer receipt (INV) and a commission note (COM)
    /// are generated per ServiceRequest.
    /// </summary>
    MarketplaceCommission = 1,

    /// <summary>
    /// Platform sells its own product or service directly to the buyer.
    /// No provider involved. Single INV invoice, no commission split.
    /// Used for CargoDry kit renewals collected by the platform.
    /// </summary>
    PrincipalSale         = 2,

    /// <summary>
    /// Recurring platform plan subscription charged to provider or participant.
    /// Single SUB invoice per billing cycle.
    /// </summary>
    SubscriptionBilling   = 3,

    /// <summary>
    /// Reserved for future fully-managed-service arrangements where Inktavia
    /// acts as both service organiser and biller. Not used in MVP.
    /// </summary>
    ManagedService        = 4,

    /// <summary>
    /// Consignment sell-through settlement: Inktavia acts as consignment seller of CargoDry kits
    /// and disburses a revenue share (ProviderPayoutAmount) to the consignment provider.
    /// Produces a ProviderSettlementStatement — not a buyer invoice, not a commission deduction.
    /// Phase 4C (July 2026).
    /// </summary>
    ConsignmentSettlement = 5,
}
