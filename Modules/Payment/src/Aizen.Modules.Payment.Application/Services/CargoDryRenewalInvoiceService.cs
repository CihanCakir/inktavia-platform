using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Application.Commands.CreateInvoiceDraft;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Implements ICargoDryRenewalInvoiceService by dispatching CreateInvoiceDraftCommand
/// via ISender (in-process MediatR).
///
/// This service bridges the module boundary: CargoDry.Application injects
/// ICargoDryRenewalInvoiceService (from Payment.Abstraction) without needing a
/// compile-time reference to Payment.Application.
///
/// Hard rules enforced:
///   #1  — Does NOT call Iyzico or any live payment provider.
///   #16 — Does NOT create a PaymentTransaction; creates a Draft invoice only.
///
/// Idempotency note: The CargoDry.Application handler (PrepareCargoDryRenewalInvoiceCommandHandler)
/// already guards against double-call by checking whether InvoiceId is already set.
/// This service therefore always creates a new draft when called — it relies on the
/// CargoDry layer to enforce the one-invoice-per-preparation constraint.
///
/// Phase 11 (July 2026).
/// </summary>
[DocumentationInfo("CargoDryRenewalInvoiceService",
    "Cross-module service that allows the CargoDry module to create a Draft CargoDryInvoice " +
    "for a kit renewal preparation by dispatching CreateInvoiceDraftCommand in-process. " +
    "Does NOT create a PaymentTransaction. Does NOT call Iyzico. " +
    "Returns the new InvoiceHeaderEntity.Id (long). Phase 11 (July 2026).")]
public sealed class CargoDryRenewalInvoiceService : ICargoDryRenewalInvoiceService
{
    private readonly ISender                                  _sender;
    private readonly ILogger<CargoDryRenewalInvoiceService>  _logger;

    public CargoDryRenewalInvoiceService(
        ISender                                 sender,
        ILogger<CargoDryRenewalInvoiceService>  logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<long> PrepareRenewalInvoiceAsync(
        long              renewalPreparationId,
        string            renewalCode,
        long              kitId,
        string            kitCode,
        string            productCode,
        string?           productName,
        long?             ownerUserId,
        decimal           renewalPrice,
        string            currencyCode,
        int               renewalMonths,
        string?           note,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Preparing renewal invoice. " +
            "RenewalPreparationId={RenewalPreparationId} RenewalCode={RenewalCode} " +
            "KitCode={KitCode} RenewalPrice={RenewalPrice} {Currency} Months={Months}",
            renewalPreparationId, renewalCode, kitCode, renewalPrice, currencyCode, renewalMonths);

        // ── Description for the single line item ──────────────────────────
        var lineDescription =
            $"CargoDry {productName ?? productCode} — Kit {kitCode} " +
            $"— {renewalMonths} month renewal ({renewalCode})";

        if (note is { Length: > 0 })
            lineDescription += $" — {note}";

        // ── Build the draft command ────────────────────────────────────────
        // CommercialModel.PrincipalSale: platform collects directly for CargoDry kit sales.
        // InvoiceType.CargoDryInvoice:  legal classification for kit renewal invoices.
        // InvoiceSourceType.CargoDry:   allows reverse-navigation from invoice to renewal.
        // SourceId = renewalPreparationId: links the invoice back to the preparation record.
        // Hard rule #16: NO PaymentTransactionId — this is a draft, not a captured transaction.
        var command = new CreateInvoiceDraftCommand
        {
            InvoiceType       = InvoiceType.CargoDryInvoice,
            CommercialModel   = CommercialModel.PrincipalSale,
            SourceType        = InvoiceSourceType.CargoDry,
            SourceId          = renewalPreparationId,

            // Seller = platform (Inktavia)
            SellerName        = "Inktavia Marine Platform",
            SellerUserId      = null,
            SellerTaxNumber   = null,
            SellerTaxOffice   = null,
            SellerAddress     = null,

            // Buyer = kit owner
            BuyerUserId       = ownerUserId,
            BuyerName         = ownerUserId.HasValue
                ? $"Owner #{ownerUserId}"
                : "CargoDry Kit Owner",
            BuyerTaxNumber    = null,
            BuyerTaxOffice    = null,
            BuyerAddress      = null,

            Currency          = currencyCode,

            // Hard rule #16: no PaymentTransaction reference in renewal notification step
            PaymentTransactionId = null,
            OriginalInvoiceId    = null,

            Lines = new List<InvoiceLineDraftItem>
            {
                new InvoiceLineDraftItem(
                    LineNumber:          1,
                    LineType:            InvoiceLineType.CargoDryRenewal,
                    Description:         lineDescription,
                    Quantity:            1m,
                    UnitPrice:           renewalPrice,
                    TaxRate:             0.20m,        // 20% KDV — standard Turkish VAT rate
                    UnitCode:            "EACH",
                    DiscountAmount:      0m,
                    ProductCode:         productCode,
                    ServiceCategoryCode: null,
                    SourceType:          InvoiceSourceType.CargoDry,
                    SourceId:            renewalPreparationId
                ),
            },
        };

        var result = await _sender.Send(command, ct)
            ?? throw new InvalidOperationException(
                $"CreateInvoiceDraftCommand returned null for renewal preparation {renewalPreparationId}.");

        _logger.LogInformation(
            "Renewal invoice draft created. " +
            "InvoiceId={InvoiceId} PublicId={PublicId} " +
            "RenewalCode={RenewalCode} KitCode={KitCode}",
            result.InvoiceId, result.PublicId, renewalCode, kitCode);

        return result.InvoiceId;
    }
}
