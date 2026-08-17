namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Classifies an individual line item within an invoice.
/// Used for template rendering, reporting aggregation, and tax rule selection.
/// </summary>
public enum InvoiceLineType
{
    /// <summary>Marine service fee charged to a participant for a completed ServiceRequest.</summary>
    ServiceFee          = 1,

    /// <summary>Platform commission deducted from provider payout.</summary>
    PlatformCommission  = 2,

    /// <summary>Provider or participant platform plan subscription charge.</summary>
    SubscriptionFee     = 3,

    /// <summary>Physical CargoDry product purchase.</summary>
    CargoDryProduct     = 4,

    /// <summary>CargoDry kit renewal fee collected by the platform.</summary>
    CargoDryRenewal     = 5,

    /// <summary>Discount applied at the invoice or line level.</summary>
    Discount            = 6,

    /// <summary>Tax line (e.g., KDV) — rendered separately for legal compliance.</summary>
    Tax                 = 7,

    /// <summary>Rounding or correction adjustment.</summary>
    Adjustment          = 8,

    /// <summary>Refund/credit line on a CreditNote or RefundInvoice.</summary>
    Refund              = 9,

    /// <summary>
    /// Settlement payout line on a ProviderSettlementStatement.
    /// Documents the provider's revenue share for a CargoDry consignment sell-through period.
    /// Tax rate = 0 (inter-party settlement; not a consumer-facing taxable sale).
    /// Phase 4C (July 2026).
    /// </summary>
    ProviderSettlementLine = 10,
}
