using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model.Request;

/// <summary>
/// HTTP body for POST /api/v1/payment/invoices.
/// Amounts at the header level are NOT provided by the caller —
/// the command handler computes SubTotal, TaxableAmount, TaxAmount, and TotalAmount
/// from the supplied line items.
/// </summary>
public sealed record CreateInvoiceDraftRequest(
    InvoiceType        InvoiceType,
    CommercialModel    CommercialModel,
    InvoiceSourceType  SourceType,
    long?              SourceId,

    // Seller party
    string             SellerName,
    long?              SellerUserId        = null,
    string?            SellerTaxNumber     = null,
    string?            SellerTaxOffice     = null,
    string?            SellerAddress       = null,

    // Buyer party
    long?              BuyerUserId         = null,
    string             BuyerName           = "—",
    string?            BuyerTaxNumber      = null,
    string?            BuyerTaxOffice      = null,
    string?            BuyerAddress        = null,

    // Currency — defaults to TRY
    string             Currency            = "TRY",

    // Cross-module linkage (all optional)
    long?              PaymentTransactionId      = null,
    long?              PaymentReleaseId          = null,
    long?              CommissionCalculationId   = null,
    long?              ProviderPayoutId          = null,
    long?              UserSubscriptionId        = null,
    long?              OriginalInvoiceId         = null,

    // Metadata
    DateTime?          DueDateUtc          = null,
    string?            Notes               = null,

    // Line items — at least one required
    List<InvoiceLineDraftRequest>? Lines   = null
);

/// <summary>
/// A single line item within a draft invoice request.
/// The handler derives LineSubTotal, TaxAmount, and LineTotal from these values.
/// </summary>
public sealed record InvoiceLineDraftRequest(
    int                LineNumber,
    InvoiceLineType    LineType,
    string             Description,
    decimal            Quantity,
    decimal            UnitPrice,
    decimal            TaxRate,
    string             UnitCode              = "EACH",
    decimal            DiscountAmount        = 0m,
    string?            ProductCode           = null,
    string?            ServiceCategoryCode   = null,
    InvoiceSourceType? SourceType            = null,
    long?              SourceId              = null
);
