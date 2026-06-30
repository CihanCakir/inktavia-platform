namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Identifies the business domain that triggered invoice generation.
/// Combined with SourceId, allows reverse-navigation from invoice back to the originating record.
///
/// Example: SourceType=ServiceRequest, SourceId=1024 → ServiceRequest #1024 details.
/// </summary>
public enum InvoiceSourceType
{
    /// <summary>Invoice generated from a completed or cancelled ServiceRequest flow.</summary>
    ServiceRequest  = 1,

    /// <summary>Invoice generated from a provider or participant plan subscription.</summary>
    Subscription    = 2,

    /// <summary>Invoice generated from a CargoDry kit renewal.</summary>
    CargoDry        = 3,

    /// <summary>Invoice generated from a provider payout event (commission deduction record).</summary>
    ProviderPayout  = 4,

    /// <summary>Invoice created manually by an Admin — no automated source entity.</summary>
    Manual          = 5,

    /// <summary>Reserved for future Commerce / marketplace order flows.</summary>
    CommerceOrder   = 6,

    /// <summary>Invoice is a refund/credit document generated from a refund event.</summary>
    Refund          = 7,
}
