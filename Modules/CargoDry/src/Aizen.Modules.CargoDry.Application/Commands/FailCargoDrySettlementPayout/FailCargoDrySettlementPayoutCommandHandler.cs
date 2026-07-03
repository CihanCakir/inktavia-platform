using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.Payment.Abstraction.Interface;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.FailCargoDrySettlementPayout;

[DocumentationInfo("FailCargoDrySettlementPayoutCommandHandler",
    "Records a payout failure on both the Payment module PayoutRecord and the CargoDry settlement. " +
    "Settlement status remains Scheduled after this call — allowing the admin to retry. " +
    "FailureReason is required. Cannot fail an already-Settled settlement. " +
    "Flow: validates prerequisites, calls ICargoDrySettlementPayoutLifecycleService.FailAsync(), " +
    "then calls settlement.MarkPayoutFailed() to record failure reason on the settlement. " +
    "No Iyzico call. Phase 4D (July 2026).")]
public sealed class FailCargoDrySettlementPayoutCommandHandler
    : AizenCommandHandler<FailCargoDrySettlementPayoutCommand, FailCargoDrySettlementPayoutResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository       _settlements;
    private readonly ICargoDrySettlementPayoutLifecycleService      _lifecycleService;
    private readonly ILogger<FailCargoDrySettlementPayoutCommandHandler> _logger;

    public FailCargoDrySettlementPayoutCommandHandler(
        ICargoDrySellThroughSettlementRepository            settlements,
        ICargoDrySettlementPayoutLifecycleService           lifecycleService,
        ILogger<FailCargoDrySettlementPayoutCommandHandler> logger)
    {
        _settlements      = settlements;
        _lifecycleService = lifecycleService;
        _logger           = logger;
    }

    public override async Task<FailCargoDrySettlementPayoutResponse> Handle(
        FailCargoDrySettlementPayoutCommand request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        // ── Load settlement ────────────────────────────────────────────────────
        var settlement = await _settlements.GetByIdAsync(request.SettlementId, ct)
            ?? throw new AizenBusinessException(
                $"Sell-through settlement {request.SettlementId} not found.");

        // ── Cannot fail an already-Settled settlement ─────────────────────────
        if (settlement.Status == CargoDrySellThroughSettlementStatus.Settled)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) is already Settled. " +
                "Cannot record payout failure for a closed settlement.");

        // ── Status guard: must be Scheduled ───────────────────────────────────
        if (settlement.Status != CargoDrySellThroughSettlementStatus.Scheduled)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) is in {settlement.Status} status. " +
                "Only Scheduled settlements can have payout failure recorded.");

        // ── Payout record must exist ──────────────────────────────────────────
        if (!settlement.PayoutRecordId.HasValue)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) has no linked PayoutRecord. " +
                "Cannot record failure without a payout record.");

        // ── Record failure in Payment module ──────────────────────────────────
        var payoutResult = await _lifecycleService.FailAsync(
            payoutRecordId:   settlement.PayoutRecordId.Value,
            failedByUserId:   request.FailedByUserId,
            failureReason:    request.FailureReason,
            externalReference: request.ExternalReference,
            note:             request.Note,
            ct:               ct);

        // ── Record failure on the settlement entity ───────────────────────────
        // MarkPayoutFailed() does NOT change Status — settlement remains Scheduled (allows retry).
        settlement.MarkPayoutFailed(
            failedByUserId: request.FailedByUserId,
            failedAtUtc:    nowUtc,
            reason:         request.FailureReason,
            note:           request.Note);

        await _settlements.SaveChangesAsync(ct);

        _logger.LogWarning(
            "CargoDry settlement payout failed. SettlementId={Id} Code={Code} " +
            "PayoutRecordId={PayoutId} Reason={Reason} Status={Status}",
            settlement.Id, settlement.SettlementCode,
            settlement.PayoutRecordId, request.FailureReason, settlement.Status);

        return new FailCargoDrySettlementPayoutResponse
        {
            Settlement   = BuildSettlementDto(settlement),
            PayoutResult = payoutResult,
        };
    }

    private static CargoDrySellThroughSettlementDto BuildSettlementDto(
        Domain.Entities.CargoDrySellThroughSettlementEntity s) => new()
    {
        Id                        = s.Id,
        PublicId                  = s.PublicId,
        SettlementCode            = s.SettlementCode,
        ConsignmentAgreementId    = s.ConsignmentAgreementId,
        ProviderProfileId         = s.ProviderProfileId,
        ProductCode               = s.ProductCode,
        BatchCode                 = s.BatchCode,
        TotalKitCount             = s.TotalKitCount,
        SettledKitCount           = s.SettledKitCount,
        TotalSaleAmount           = s.TotalSaleAmount,
        TotalCommissionAmount     = s.TotalCommissionAmount,
        ProviderPayoutAmount      = s.ProviderPayoutAmount,
        CurrencyCode              = s.CurrencyCode,
        PeriodStartUtc            = s.PeriodStartUtc,
        PeriodEndUtc              = s.PeriodEndUtc,
        Status                    = s.Status,
        StatusName                = s.Status.ToString(),
        ScheduledSettlementDate   = s.ScheduledSettlementDate,
        SettledAtUtc              = s.SettledAtUtc,
        SettledByUserId           = s.SettledByUserId,
        DisputeReason             = s.DisputeReason,
        Note                      = s.Note,
        ReadyForSettlementAtUtc   = s.ReadyForSettlementAtUtc,
        PayoutRecordId            = s.PayoutRecordId,
        PaymentPreparedAtUtc      = s.PaymentPreparedAtUtc,
        PaymentPreparedByUserId   = s.PaymentPreparedByUserId,
        PaymentPreparationNote    = s.PaymentPreparationNote,
        InvoiceId                 = s.InvoiceId,
        InvoicePreparedAtUtc      = s.InvoicePreparedAtUtc,
        InvoicePreparedByUserId   = s.InvoicePreparedByUserId,
        InvoicePreparationNote    = s.InvoicePreparationNote,
        PayoutCompletedAtUtc      = s.PayoutCompletedAtUtc,
        PayoutCompletedByUserId   = s.PayoutCompletedByUserId,
        PayoutCompletionReference = s.PayoutCompletionReference,
        PayoutFailureReason       = s.PayoutFailureReason,
        PayoutLifecycleNote       = s.PayoutLifecycleNote,
        CreatedAtUtc              = s.CreatedAtUtc,
    };
}
