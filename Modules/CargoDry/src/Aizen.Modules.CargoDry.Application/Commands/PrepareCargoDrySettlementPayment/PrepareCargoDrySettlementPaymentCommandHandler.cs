using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.Payment.Abstraction.Interface;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDrySettlementPayment;

[DocumentationInfo("PrepareCargoDrySettlementPaymentCommandHandler",
    "Prepares a payout payment record for a CargoDry sell-through settlement " +
    "that is in ReadyForSettlement status. " +
    "Dispatches to the Payment module via ICargoDrySettlementPayoutService (in-process, " +
    "bridged through Payment.Abstraction to avoid a direct Payment.Application dependency). " +
    "Idempotent: if the settlement already has a PayoutRecordId, returns existing data. " +
    "Does NOT create PaymentTransaction, Invoice, or execute any Iyzico transfer. " +
    "Phase 4B (July 2026).")]
public sealed class PrepareCargoDrySettlementPaymentCommandHandler
    : AizenCommandHandler<PrepareCargoDrySettlementPaymentCommand, PrepareCargoDrySettlementPaymentResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository       _settlements;
    private readonly ICargoDrySettlementPayoutService               _payoutService;
    private readonly ILogger<PrepareCargoDrySettlementPaymentCommandHandler> _logger;

    public PrepareCargoDrySettlementPaymentCommandHandler(
        ICargoDrySellThroughSettlementRepository              settlements,
        ICargoDrySettlementPayoutService                      payoutService,
        ILogger<PrepareCargoDrySettlementPaymentCommandHandler> logger)
    {
        _settlements   = settlements;
        _payoutService = payoutService;
        _logger        = logger;
    }

    public override async Task<PrepareCargoDrySettlementPaymentResponse> Handle(
        PrepareCargoDrySettlementPaymentCommand request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        // ── Load settlement ────────────────────────────────────────────────────
        var settlement = await _settlements.GetByIdAsync(request.SettlementId, ct)
            ?? throw new AizenBusinessException(
                $"Sell-through settlement {request.SettlementId} not found.");

        // ── Idempotency guard (settlement level) ───────────────────────────────
        if (settlement.PayoutRecordId.HasValue)
        {
            _logger.LogInformation(
                "Settlement {Id} ({Code}) already has a payout record prepared. PayoutRecordId={PayoutId}",
                settlement.Id, settlement.SettlementCode, settlement.PayoutRecordId.Value);

            return BuildResponse(settlement, settlement.PayoutRecordId.Value, alreadyExisted: true);
        }

        // ── Status guard ───────────────────────────────────────────────────────
        if (settlement.Status != CargoDrySellThroughSettlementStatus.ReadyForSettlement)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) is in status " +
                $"{settlement.Status} — only ReadyForSettlement settlements can have payment prepared.");

        // ── Validate payout amount ─────────────────────────────────────────────
        if (settlement.ProviderPayoutAmount <= 0)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) has a zero or negative " +
                $"ProviderPayoutAmount ({settlement.ProviderPayoutAmount} {settlement.CurrencyCode}). " +
                "Cannot prepare payout for a zero-amount settlement.");

        // ── Build description ──────────────────────────────────────────────────
        var description = $"CargoDry sell-through settlement payout — {settlement.SettlementCode} " +
                          $"({settlement.ProductCode}, {settlement.CurrencyCode}, " +
                          $"{settlement.PeriodStartUtc:yyyy-MM} — {settlement.TotalKitCount} kits)";

        // ── Dispatch to Payment module in-process ──────────────────────────────
        var payoutResult = await _payoutService.PrepareSettlementPayoutAsync(
            settlementId:     settlement.Id,
            settlementCode:   settlement.SettlementCode,
            providerProfileId: settlement.ProviderProfileId,
            amount:           settlement.ProviderPayoutAmount,
            currencyCode:     settlement.CurrencyCode,
            description:      description,
            preparedByUserId: request.PreparedByUserId,
            ct:               ct);

        // ── Mark settlement as payment prepared ────────────────────────────────
        // MarkPaymentPrepared() transitions ReadyForSettlement → Scheduled
        settlement.MarkPaymentPrepared(
            payoutRecordId:   payoutResult.PayoutRecordId,
            preparedByUserId: request.PreparedByUserId,
            preparedAtUtc:    nowUtc,
            note:             request.PreparationNote);

        await _settlements.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CargoDry settlement payment prepared. SettlementId={Id} Code={Code} " +
            "PayoutRecordId={PayoutId} AlreadyExisted={AlreadyExisted}",
            settlement.Id, settlement.SettlementCode,
            payoutResult.PayoutRecordId, payoutResult.AlreadyExisted);

        return BuildResponse(settlement, payoutResult.PayoutRecordId, payoutResult.AlreadyExisted);
    }

    private static PrepareCargoDrySettlementPaymentResponse BuildResponse(
        Domain.Entities.CargoDrySellThroughSettlementEntity settlement,
        long   payoutRecordId,
        bool   alreadyExisted)
    {
        return new PrepareCargoDrySettlementPaymentResponse
        {
            PayoutRecordId = payoutRecordId,
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
                PayoutRecordId          = settlement.PayoutRecordId,
                PaymentPreparedAtUtc    = settlement.PaymentPreparedAtUtc,
                PaymentPreparedByUserId = settlement.PaymentPreparedByUserId,
                PaymentPreparationNote  = settlement.PaymentPreparationNote,
                // Phase 4C (fields will be null at Phase 4B call time)
                InvoiceId               = settlement.InvoiceId,
                InvoicePreparedAtUtc    = settlement.InvoicePreparedAtUtc,
                InvoicePreparedByUserId = settlement.InvoicePreparedByUserId,
                InvoicePreparationNote  = settlement.InvoicePreparationNote,
                CreatedAtUtc            = settlement.CreatedAtUtc,
            }
        };
    }
}
