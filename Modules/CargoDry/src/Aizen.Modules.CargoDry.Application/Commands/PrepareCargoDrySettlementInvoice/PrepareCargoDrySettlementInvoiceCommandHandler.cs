using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.Payment.Abstraction.Interface;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDrySettlementInvoice;

[DocumentationInfo("PrepareCargoDrySettlementInvoiceCommandHandler",
    "Prepares a Draft ProviderSettlementStatement invoice for a CargoDry sell-through settlement " +
    "in Scheduled status. Dispatches to Payment module via ICargoDrySettlementInvoiceService " +
    "(in-process, bridged through Payment.Abstraction to avoid a direct Payment.Application dependency). " +
    "Idempotent: if settlement.InvoiceId is already set, returns existing data without re-creating. " +
    "Settlement status remains Scheduled after preparation (Option B lifecycle). " +
    "Does NOT issue the invoice, does NOT create PaymentTransaction. " +
    "Phase 4C (July 2026).")]
public sealed class PrepareCargoDrySettlementInvoiceCommandHandler
    : AizenCommandHandler<PrepareCargoDrySettlementInvoiceCommand, PrepareCargoDrySettlementInvoiceResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository        _settlements;
    private readonly ICargoDrySettlementInvoiceService               _invoiceService;
    private readonly ILogger<PrepareCargoDrySettlementInvoiceCommandHandler> _logger;

    public PrepareCargoDrySettlementInvoiceCommandHandler(
        ICargoDrySellThroughSettlementRepository               settlements,
        ICargoDrySettlementInvoiceService                      invoiceService,
        ILogger<PrepareCargoDrySettlementInvoiceCommandHandler> logger)
    {
        _settlements    = settlements;
        _invoiceService = invoiceService;
        _logger         = logger;
    }

    public override async Task<PrepareCargoDrySettlementInvoiceResponse> Handle(
        PrepareCargoDrySettlementInvoiceCommand request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        // ── Load settlement ────────────────────────────────────────────────────
        var settlement = await _settlements.GetByIdAsync(request.SettlementId, ct)
            ?? throw new AizenBusinessException(
                $"Sell-through settlement {request.SettlementId} not found.");

        // ── Idempotency guard (settlement level) ───────────────────────────────
        if (settlement.InvoiceId.HasValue)
        {
            _logger.LogInformation(
                "Settlement {Id} ({Code}) already has an invoice prepared. InvoiceId={InvoiceId}",
                settlement.Id, settlement.SettlementCode, settlement.InvoiceId.Value);

            return BuildResponse(settlement, settlement.InvoiceId.Value, alreadyExisted: true);
        }

        // ── Status guard: must be Scheduled ───────────────────────────────────
        if (settlement.Status != CargoDrySellThroughSettlementStatus.Scheduled)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) is in status " +
                $"{settlement.Status} — only Scheduled settlements can have invoice prepared. " +
                "Run PrepareCargoDrySettlementPayment (Phase 4B) first to move settlement to Scheduled status.");

        // ── Validate payout amount ─────────────────────────────────────────────
        if (settlement.ProviderPayoutAmount <= 0)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) has a zero or negative " +
                $"ProviderPayoutAmount ({settlement.ProviderPayoutAmount} {settlement.CurrencyCode}). " +
                "Cannot prepare invoice for a zero-amount settlement.");

        // ── Dispatch to Payment module in-process ──────────────────────────────
        var invoiceResult = await _invoiceService.PrepareSettlementStatementAsync(
            settlementId:         settlement.Id,
            settlementCode:       settlement.SettlementCode,
            providerProfileId:    settlement.ProviderProfileId,
            providerPayoutAmount: settlement.ProviderPayoutAmount,
            totalSaleAmount:      settlement.TotalSaleAmount,
            totalCommissionAmount: settlement.TotalCommissionAmount,
            totalKitCount:        settlement.TotalKitCount,
            currencyCode:         settlement.CurrencyCode,
            productCode:          settlement.ProductCode,
            periodStartUtc:       settlement.PeriodStartUtc,
            periodEndUtc:         settlement.PeriodEndUtc,
            payoutRecordId:       settlement.PayoutRecordId,
            preparedByUserId:     request.PreparedByUserId,
            notes:                request.PreparationNote,
            ct:                   ct);

        // ── Mark settlement invoice prepared ───────────────────────────────────
        // MarkInvoicePrepared() does NOT change Status — settlement remains Scheduled.
        settlement.MarkInvoicePrepared(
            invoiceId:        invoiceResult.InvoiceId,
            preparedByUserId: request.PreparedByUserId,
            preparedAtUtc:    nowUtc,
            note:             request.PreparationNote);

        await _settlements.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CargoDry settlement invoice prepared. SettlementId={Id} Code={Code} " +
            "InvoiceId={InvoiceId} Status={Status} AlreadyExisted={AlreadyExisted}",
            settlement.Id, settlement.SettlementCode,
            invoiceResult.InvoiceId, invoiceResult.InvoiceStatus, invoiceResult.AlreadyExisted);

        return BuildResponse(settlement, invoiceResult.InvoiceId, invoiceResult.AlreadyExisted);
    }

    private static PrepareCargoDrySettlementInvoiceResponse BuildResponse(
        Domain.Entities.CargoDrySellThroughSettlementEntity settlement,
        long invoiceId,
        bool alreadyExisted)
    {
        return new PrepareCargoDrySettlementInvoiceResponse
        {
            InvoiceId      = invoiceId,
            AlreadyExisted = alreadyExisted,
            Settlement     = new CargoDrySellThroughSettlementDto
            {
                Id                      = settlement.Id,
                PublicId                = settlement.PublicId,
                SettlementCode          = settlement.SettlementCode,
                ConsignmentAgreementId  = settlement.ConsignmentAgreementId,
                ProviderProfileId       = settlement.ProviderProfileId,
                ProductCode             = settlement.ProductCode,
                BatchCode               = settlement.BatchCode,
                TotalKitCount           = settlement.TotalKitCount,
                SettledKitCount         = settlement.SettledKitCount,
                TotalSaleAmount         = settlement.TotalSaleAmount,
                TotalCommissionAmount   = settlement.TotalCommissionAmount,
                ProviderPayoutAmount    = settlement.ProviderPayoutAmount,
                CurrencyCode            = settlement.CurrencyCode,
                PeriodStartUtc          = settlement.PeriodStartUtc,
                PeriodEndUtc            = settlement.PeriodEndUtc,
                Status                  = settlement.Status,
                StatusName              = settlement.Status.ToString(),
                ScheduledSettlementDate = settlement.ScheduledSettlementDate,
                SettledAtUtc            = settlement.SettledAtUtc,
                SettledByUserId         = settlement.SettledByUserId,
                DisputeReason           = settlement.DisputeReason,
                Note                    = settlement.Note,
                ReadyForSettlementAtUtc = settlement.ReadyForSettlementAtUtc,
                // Phase 4B
                PayoutRecordId          = settlement.PayoutRecordId,
                PaymentPreparedAtUtc    = settlement.PaymentPreparedAtUtc,
                PaymentPreparedByUserId = settlement.PaymentPreparedByUserId,
                PaymentPreparationNote  = settlement.PaymentPreparationNote,
                // Phase 4C
                InvoiceId               = settlement.InvoiceId,
                InvoicePreparedAtUtc    = settlement.InvoicePreparedAtUtc,
                InvoicePreparedByUserId = settlement.InvoicePreparedByUserId,
                InvoicePreparationNote  = settlement.InvoicePreparationNote,
                CreatedAtUtc            = settlement.CreatedAtUtc,
            }
        };
    }
}
