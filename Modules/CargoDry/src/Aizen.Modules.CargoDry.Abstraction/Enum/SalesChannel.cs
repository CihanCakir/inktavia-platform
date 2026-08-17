namespace Aizen.Modules.CargoDry.Abstraction.Enum;

/// <summary>
/// Describes how a CargoDry kit reached the end user or provider.
/// Drives commercial model selection, invoice generation, and payout rules.
///
/// Decision lock (July 2026):
///   N3 — ConsignmentSellThrough is the default when stock is sent to a provider.
///   N4 — ProviderResale only when provider paid upfront (wholesale invoice at batch purchase).
///   N5 — DirectSale when Inktavia sells through the platform.
///   N7 — ProviderAttributedSale when buyer pays Inktavia but a provider assisted the sale.
/// </summary>
public enum SalesChannel
{
    /// <summary>
    /// Inktavia sells directly to participant or provider through the platform.
    /// Single CargoDryInvoice to buyer. No provider payout.
    /// CommercialModel = PrincipalSale.
    /// </summary>
    DirectSale           = 1,

    /// <summary>
    /// Provider paid Inktavia upfront for a batch (wholesale price).
    /// Provider sells to end buyer outside the platform.
    /// Wholesale invoice issued at batch purchase — no activation-time invoice.
    /// CommercialModel = MarketplaceCommission (Inktavia earns wholesale margin).
    /// </summary>
    ProviderResale       = 2,

    /// <summary>
    /// Provider holds stock on consignment — ownership remains with Inktavia.
    /// Revenue recognised only on kit activation by end user.
    /// CargoDrySellThroughSettlementEntity created; settled in weekly batch.
    /// CommercialModel = PrincipalSale.
    /// </summary>
    ConsignmentSellThrough = 3,

    /// <summary>
    /// Buyer pays Inktavia directly, but a provider is credited for facilitating the sale.
    /// CargoDryInvoice to buyer + CommissionInvoice/PayoutRecord to provider.
    /// CommercialModel = MarketplaceCommission.
    /// </summary>
    ProviderAttributedSale = 4,
}
