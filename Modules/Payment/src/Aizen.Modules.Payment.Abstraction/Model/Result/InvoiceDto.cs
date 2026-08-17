using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Abstraction.Model.Result;

/// <summary>
/// Full invoice detail DTO — used for single-invoice GET responses.
/// Lines and TaxBreakdowns are populated only when loaded via GetByIdFullAsync.
/// </summary>
public sealed record InvoiceDto(
    long              Id,
    string?           InvoiceNumber,
    InvoiceType       InvoiceType,
    CommercialModel   CommercialModel,
    BillingMode       BillingMode,
    InvoiceStatus     Status,
    InvoiceSourceType SourceType,
    long?             SourceId,

    // Payment linkage
    long?             PaymentTransactionId,
    long?             PaymentReleaseId,
    long?             CommissionCalculationId,
    long?             ProviderPayoutId,
    long?             UserSubscriptionId,
    long?             OriginalInvoiceId,

    // Seller
    long?             SellerUserId,
    string            SellerName,
    string?           SellerTaxNumber,
    string?           SellerTaxOffice,
    string?           SellerAddress,

    // Buyer
    long?             BuyerUserId,
    string            BuyerName,
    string?           BuyerTaxNumber,
    string?           BuyerTaxOffice,
    string?           BuyerAddress,

    // Amounts
    string            Currency,
    decimal           SubTotalAmount,
    decimal           DiscountAmount,
    decimal           TaxableAmount,
    decimal           TaxAmount,
    decimal           TotalAmount,
    decimal           PaidAmount,
    decimal           RemainingAmount,

    // Dates
    DateTime?         IssueDateUtc,
    DateTime?         DueDateUtc,
    DateTime?         PaidAtUtc,
    DateTime?         CancelledAtUtc,
    DateTime?         CreateDate,

    // External / PDF
    string?           ExternalInvoiceId,
    string?           ExternalInvoiceProvider,
    string?           PdfFileRef,
    string?           Notes,

    // Children (null when loaded via header-only query)
    IReadOnlyList<InvoiceLineDto>?         Lines,
    IReadOnlyList<InvoiceTaxBreakdownDto>? TaxBreakdowns
);

/// <summary>
/// Lightweight DTO for invoice list views — no line items or tax breakdowns.
/// </summary>
public sealed record InvoiceListItemDto(
    long              Id,
    string?           InvoiceNumber,
    InvoiceType       InvoiceType,
    InvoiceStatus     Status,
    long?             BuyerUserId,
    string            BuyerName,
    string            Currency,
    decimal           TotalAmount,
    decimal           PaidAmount,
    decimal           RemainingAmount,
    DateTime?         IssueDateUtc,
    DateTime?         DueDateUtc,
    DateTime?         CreateDate
);

/// <summary>Paged invoice list response.</summary>
public sealed record InvoiceListResult(
    List<InvoiceListItemDto> Items,
    int                      Total,
    int                      Page,
    int                      PageSize
);
