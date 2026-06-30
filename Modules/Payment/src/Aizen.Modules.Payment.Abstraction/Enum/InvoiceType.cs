namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Classifies the legal and commercial type of an invoice document.
/// Drives prefix assignment, template selection, and legal note requirements.
/// </summary>
public enum InvoiceType
{
    /// <summary>Standard sale invoice: buyer pays platform for a service or product.</summary>
    SalesInvoice       = 1,

    /// <summary>Platform commission deducted from provider payout after job completion.</summary>
    CommissionInvoice  = 2,

    /// <summary>Recurring subscription billing (provider plan or participant plan).</summary>
    SubscriptionInvoice = 3,

    /// <summary>Invoice for a CargoDry kit renewal payment collected by the platform.</summary>
    CargoDryInvoice    = 4,

    /// <summary>
    /// Credit note offsetting a previously issued invoice.
    /// Always linked to an original invoice via OriginalInvoiceId.
    /// </summary>
    CreditNote         = 5,

    /// <summary>Refund document for a payment that has been returned to the buyer.</summary>
    RefundInvoice      = 6,

    /// <summary>Non-binding proforma invoice; not a legal tax document.</summary>
    ProformaInvoice    = 7,
}
