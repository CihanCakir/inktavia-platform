using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CreateInvoiceDraft;

/// <summary>
/// Creates a Draft invoice with line items and tax breakdowns.
///
/// ── Amount computation ────────────────────────────────────────────────────────
///  SubTotal     = Σ (Qty × UnitPrice)
///  DiscountAmt  = Σ DiscountAmount per line
///  TaxableAmt   = SubTotal - DiscountAmt
///  TaxAmount    = Σ (LineSubTotal - Discount) × TaxRate per line  (4 dp, AwayFromZero)
///  TotalAmount  = TaxableAmt + TaxAmount
///
/// ── Tax breakdown aggregation ─────────────────────────────────────────────────
///  Lines are grouped by TaxRate. For each distinct rate a single
///  InvoiceTaxBreakdownEntity row is created with the summed TaxableAmount and TaxAmount.
///  TaxType string is derived as "KDV{rate%}" (e.g. 0.20 → "KDV20").
///
/// ── SaveChanges ───────────────────────────────────────────────────────────────
///  NOT called here — delegated to AizenCommandHandlerDecorator (UnitOfWork pattern).
/// </summary>
[DocumentationInfo("Create invoice draft command handler",
    "Creates a Draft InvoiceHeaderEntity with computed totals, line items, and tax breakdown rows. " +
    "Does not assign an invoice number — that happens at Issue.")]
public sealed class CreateInvoiceDraftCommandHandler
    : AizenCommandHandler<CreateInvoiceDraftCommand, CreateInvoiceDraftResult>
{
    private readonly IInvoiceRepository                          _invoices;
    private readonly ILogger<CreateInvoiceDraftCommandHandler>  _logger;

    public CreateInvoiceDraftCommandHandler(
        IAizenUnitOfWork<PaymentDbContext>           unitOfWork,
        IInvoiceRepository                           invoices,
        ILogger<CreateInvoiceDraftCommandHandler>   logger)
    {
        _invoices = invoices;
        _logger   = logger;
    }

    public override async Task<CreateInvoiceDraftResult?> Handle(
        CreateInvoiceDraftCommand request, CancellationToken ct)
    {
        // ── 1. Compute header totals from line items ───────────────────────────
        var subTotal    = 0m;
        var discountAmt = 0m;
        var taxAmount   = 0m;

        foreach (var line in request.Lines)
        {
            var lineSubTotal = line.Quantity * line.UnitPrice;
            var lineNet      = lineSubTotal - line.DiscountAmount;
            var lineTax      = Math.Round(lineNet * line.TaxRate, 4, MidpointRounding.AwayFromZero);

            subTotal    += lineSubTotal;
            discountAmt += line.DiscountAmount;
            taxAmount   += lineTax;
        }

        var taxableAmt  = subTotal - discountAmt;
        var totalAmount = taxableAmt + taxAmount;

        if (totalAmount < 0)
            throw new AizenBusinessException((int)PaymentErrorCode.CommissionDiscountExceedsGross);

        // ── 2. Create the aggregate root (Draft, no number) ───────────────────
        var invoice = InvoiceHeaderEntity.CreateDraft(
            invoiceType:            request.InvoiceType,
            commercialModel:        request.CommercialModel,
            sourceType:             request.SourceType,
            sourceId:               request.SourceId,
            sellerName:             request.SellerName,
            buyerName:              request.BuyerName,
            currency:               request.Currency,
            subTotalAmount:         subTotal,
            discountAmount:         discountAmt,
            taxableAmount:          taxableAmt,
            taxAmount:              taxAmount,
            totalAmount:            totalAmount,
            sellerUserId:           request.SellerUserId,
            sellerTaxNumber:        request.SellerTaxNumber,
            sellerTaxOffice:        request.SellerTaxOffice,
            sellerAddress:          request.SellerAddress,
            buyerUserId:            request.BuyerUserId,
            buyerTaxNumber:         request.BuyerTaxNumber,
            buyerTaxOffice:         request.BuyerTaxOffice,
            buyerAddress:           request.BuyerAddress,
            paymentTransactionId:   request.PaymentTransactionId,
            paymentReleaseId:       request.PaymentReleaseId,
            commissionCalculationId: request.CommissionCalculationId,
            providerPayoutId:       request.ProviderPayoutId,
            userSubscriptionId:     request.UserSubscriptionId,
            originalInvoiceId:      request.OriginalInvoiceId,
            dueDateUtc:             request.DueDateUtc,
            notes:                  request.Notes);

        await _invoices.AddAsync(invoice, ct);
        // EF will flush after handler returns via decorator — FK (InvoiceHeaderId) is set below
        // using EF shadow FK conventions; lines are added to the navigation collection.

        // ── 3. Add line items ─────────────────────────────────────────────────
        for (var i = 0; i < request.Lines.Count; i++)
        {
            var req  = request.Lines[i];
            var line = InvoiceLineEntity.Create(
                invoiceHeaderId:      0,   // EF assigns FK from navigation parent
                lineNumber:           req.LineNumber > 0 ? req.LineNumber : i + 1,
                lineType:             req.LineType,
                description:          req.Description,
                quantity:             req.Quantity,
                unitPrice:            req.UnitPrice,
                taxRate:              req.TaxRate,
                unitCode:             req.UnitCode,
                discountAmount:       req.DiscountAmount,
                productCode:          req.ProductCode,
                serviceCategoryCode:  req.ServiceCategoryCode,
                sourceType:           req.SourceType,
                sourceId:             req.SourceId);

            invoice.AddLine(line);
        }

        // ── 4. Aggregate tax breakdown rows (one per distinct TaxRate) ─────────
        var taxGroups = request.Lines
            .GroupBy(l => l.TaxRate)
            .Select(g =>
            {
                var groupTaxableAmt = g.Sum(l =>
                    Math.Round((l.Quantity * l.UnitPrice) - l.DiscountAmount, 4, MidpointRounding.AwayFromZero));

                var taxType = $"KDV{(int)Math.Round(g.Key * 100)}";

                return InvoiceTaxBreakdownEntity.Create(
                    invoiceHeaderId: 0,   // EF assigns from parent nav
                    taxType:         taxType,
                    taxRate:         g.Key,
                    taxableAmount:   groupTaxableAmt);
            });

        foreach (var breakdown in taxGroups)
            invoice.AddTaxBreakdown(breakdown);

        _logger.LogInformation(
            "Invoice draft created. Type={Type} Buyer={Buyer} Total={Total} {Currency}",
            request.InvoiceType, request.BuyerName, totalAmount, request.Currency);

        return new CreateInvoiceDraftResult(invoice.Id, invoice.PublicId);
    }
}
