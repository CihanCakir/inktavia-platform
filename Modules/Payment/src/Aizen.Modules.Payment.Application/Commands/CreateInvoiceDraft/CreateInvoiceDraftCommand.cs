using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.CreateInvoiceDraft;

/// <summary>
/// Creates a Draft invoice header with its line items and tax breakdowns.
/// Financial totals (SubTotal, TaxableAmount, TaxAmount, TotalAmount) are computed
/// inside the handler from the provided line items — callers must NOT pre-compute them.
///
/// InvoiceNumber is NOT assigned at this stage.
/// Use IssueInvoiceCommand after draft creation to assign a number and transition to Issued.
/// </summary>
public sealed class CreateInvoiceDraftCommand : AizenCommand<CreateInvoiceDraftResult>
{
    public required InvoiceType       InvoiceType                  { get; init; }
    public required CommercialModel   CommercialModel              { get; init; }
    public required InvoiceSourceType SourceType                   { get; init; }
    public          long?             SourceId                     { get; init; }

    // Seller
    public required string            SellerName                   { get; init; }
    public          long?             SellerUserId                 { get; init; }
    public          string?           SellerTaxNumber              { get; init; }
    public          string?           SellerTaxOffice              { get; init; }
    public          string?           SellerAddress                { get; init; }

    // Buyer
    public          long?             BuyerUserId                  { get; init; }
    public required string            BuyerName                    { get; init; }
    public          string?           BuyerTaxNumber               { get; init; }
    public          string?           BuyerTaxOffice               { get; init; }
    public          string?           BuyerAddress                 { get; init; }

    // Currency
    public required string            Currency                     { get; init; }

    // Cross-module linkage
    public          long?             PaymentTransactionId         { get; init; }
    public          long?             PaymentReleaseId             { get; init; }
    public          long?             CommissionCalculationId      { get; init; }
    public          long?             ProviderPayoutId             { get; init; }
    public          long?             UserSubscriptionId           { get; init; }
    public          long?             OriginalInvoiceId            { get; init; }

    public          DateTime?         DueDateUtc                   { get; init; }
    public          string?           Notes                        { get; init; }

    /// <summary>At least one line item is required (validated by FluentValidation).</summary>
    public required List<InvoiceLineDraftItem> Lines               { get; init; }
}

/// <summary>Embedded line item data — mirrors InvoiceLineDraftRequest from the Abstraction layer.</summary>
public sealed record InvoiceLineDraftItem(
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
