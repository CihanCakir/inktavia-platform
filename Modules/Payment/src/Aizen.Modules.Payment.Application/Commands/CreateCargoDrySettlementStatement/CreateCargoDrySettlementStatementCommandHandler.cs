using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Domain.Entities.Invoice;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.CreateCargoDrySettlementStatement;

[DocumentationInfo("CreateCargoDrySettlementStatementCommandHandler",
    "Creates a Draft ProviderSettlementStatement invoice for a CargoDry sell-through settlement. " +
    "Dispatched in-process from the CargoDry module via ISender. " +
    "Idempotent by (InvoiceSourceType.CargoDrySettlement, SettlementId). " +
    "Does NOT issue the invoice, does NOT create a PaymentTransaction. " +
    "Phase 4C (July 2026).")]
public sealed class CreateCargoDrySettlementStatementCommandHandler
    : AizenCommandHandler<CreateCargoDrySettlementStatementCommand, CreateCargoDrySettlementStatementResult>
{
    private readonly IInvoiceRepository                                            _invoices;
    private readonly ILogger<CreateCargoDrySettlementStatementCommandHandler>      _logger;

    public CreateCargoDrySettlementStatementCommandHandler(
        IInvoiceRepository                                           invoices,
        ILogger<CreateCargoDrySettlementStatementCommandHandler>     logger)
    {
        _invoices = invoices;
        _logger   = logger;
    }

    public override async Task<CreateCargoDrySettlementStatementResult?> Handle(
        CreateCargoDrySettlementStatementCommand request, CancellationToken ct)
    {
        // ── Idempotency guard ─────────────────────────────────────────────────
        var alreadyExists = await _invoices.ExistsForSourceAsync(
            InvoiceSourceType.CargoDrySettlement, request.SettlementId, ct);

        if (alreadyExists)
        {
            var existing = await _invoices.GetBySourceAsync(
                InvoiceSourceType.CargoDrySettlement, request.SettlementId, ct);

            _logger.LogInformation(
                "ProviderSettlementStatement already exists for settlement. " +
                "SettlementId={SettlementId} InvoiceId={InvoiceId} Status={Status}",
                request.SettlementId, existing!.Id, existing.Status);

            return new CreateCargoDrySettlementStatementResult(
                InvoiceId:     existing.Id,
                InvoiceNumber: existing.InvoiceNumber,
                InvoiceStatus: existing.Status,
                AlreadyExisted: true);
        }

        // ── Build settlement statement description ────────────────────────────
        var periodLabel = $"{request.PeriodStartUtc:yyyy-MM-dd} – {request.PeriodEndUtc:yyyy-MM-dd}";
        var headerNotes = $"CargoDry consignment settlement {request.SettlementCode} — " +
                          $"period {periodLabel}. " +
                          $"TotalSales={request.TotalSaleAmount:F2} {request.CurrencyCode}, " +
                          $"Commission={request.TotalCommissionAmount:F2} {request.CurrencyCode}, " +
                          $"Kits={request.TotalKitCount}. " +
                          (string.IsNullOrWhiteSpace(request.Notes) ? "" : request.Notes);

        // ── Create Draft header ───────────────────────────────────────────────
        // Seller = Inktavia (platform acts as consignment seller; SellerUserId = null)
        // Buyer  = consignment provider (receives payout)
        // TaxAmount = 0 — inter-party settlement; tax rate confirmed as 0 by accountant
        var providerPayoutAmount = request.ProviderPayoutAmount;

        var invoiceHeader = InvoiceHeaderEntity.CreateDraft(
            invoiceType:           InvoiceType.ProviderSettlementStatement,
            commercialModel:       CommercialModel.ConsignmentSettlement,
            sourceType:            InvoiceSourceType.CargoDrySettlement,
            sourceId:              request.SettlementId,
            sellerName:            "Inktavia",
            buyerName:             $"Provider #{request.ProviderProfileId}",
            currency:              request.CurrencyCode,
            subTotalAmount:        providerPayoutAmount,
            discountAmount:        0m,
            taxableAmount:         providerPayoutAmount,
            taxAmount:             0m,                          // TaxRate = 0 for settlement statements
            totalAmount:           providerPayoutAmount,
            sellerUserId:          null,                        // Inktavia is the seller
            buyerUserId:           request.ProviderProfileId,
            providerPayoutId:      request.PayoutRecordId,      // Phase 4B link
            notes:                 headerNotes);

        // ── Create settlement line ────────────────────────────────────────────
        var lineDescription = $"Provider revenue share — {request.ProductCode} " +
                              $"consignment settlement {request.SettlementCode}, " +
                              $"{request.TotalKitCount} kits, period {periodLabel}";

        var settlementLine = InvoiceLineEntity.Create(
            invoiceHeaderId:  invoiceHeader.Id,
            lineNumber:       1,
            lineType:         InvoiceLineType.ProviderSettlementLine,
            description:      lineDescription,
            quantity:         request.TotalKitCount,
            unitPrice:        request.TotalKitCount > 0
                                  ? Math.Round(providerPayoutAmount / request.TotalKitCount, 4, MidpointRounding.AwayFromZero)
                                  : providerPayoutAmount,
            taxRate:          0m,                              // inter-party; no consumer VAT
            unitCode:         "KIT",
            discountAmount:   0m,
            productCode:      request.ProductCode,
            sourceType:       InvoiceSourceType.CargoDrySettlement,
            sourceId:         request.SettlementId);

        invoiceHeader.AddLine(settlementLine);

        await _invoices.AddAsync(invoiceHeader, ct);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "ProviderSettlementStatement Draft created. " +
            "SettlementId={SettlementId} Code={Code} InvoiceId={InvoiceId} " +
            "ProviderPayoutAmount={Amount} {Currency}",
            request.SettlementId, request.SettlementCode, invoiceHeader.Id,
            providerPayoutAmount, request.CurrencyCode);

        return new CreateCargoDrySettlementStatementResult(
            InvoiceId:      invoiceHeader.Id,
            InvoiceNumber:  null,    // Draft — number assigned only at Issue
            InvoiceStatus:  invoiceHeader.Status,
            AlreadyExisted: false);
    }
}
