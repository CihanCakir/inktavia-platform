using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.Payment.Abstraction.Interface;

namespace Aizen.Modules.CargoDry.Application.Commands.MarkCargoDrySettlementPayoutProcessing;

[DocumentationInfo("MarkCargoDrySettlementPayoutProcessingCommandHandler",
    "Validates the settlement is in Scheduled status with an existing PayoutRecord, " +
    "then calls ICargoDrySettlementPayoutLifecycleService.MarkProcessingAsync() to transition " +
    "the PayoutRecord to Processing status. " +
    "Settlement status remains Scheduled after this call. Optional step between Approve and Complete. " +
    "No Iyzico call. No bank transfer. Phase 4D (July 2026).")]
public sealed class MarkCargoDrySettlementPayoutProcessingCommandHandler
    : AizenCommandHandler<MarkCargoDrySettlementPayoutProcessingCommand, MarkCargoDrySettlementPayoutProcessingResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;
    private readonly ICargoDrySettlementPayoutLifecycleService _lifecycleService;

    public MarkCargoDrySettlementPayoutProcessingCommandHandler(
        ICargoDrySellThroughSettlementRepository settlements,
        ICargoDrySettlementPayoutLifecycleService lifecycleService)
    {
        _settlements      = settlements;
        _lifecycleService = lifecycleService;
    }

    public override async Task<MarkCargoDrySettlementPayoutProcessingResponse> Handle(
        MarkCargoDrySettlementPayoutProcessingCommand request, CancellationToken ct)
    {
        // ── Load settlement ────────────────────────────────────────────────────
        var settlement = await _settlements.GetByIdAsync(request.SettlementId, ct)
            ?? throw new AizenBusinessException(
                $"Sell-through settlement {request.SettlementId} not found.");

        // ── Status guard: must be Scheduled ───────────────────────────────────
        if (settlement.Status != CargoDrySellThroughSettlementStatus.Scheduled)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) is in {settlement.Status} status. " +
                "Only Scheduled settlements can have payout marked as processing.");

        // ── Payout record must exist ──────────────────────────────────────────
        if (!settlement.PayoutRecordId.HasValue)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) has no linked PayoutRecord. " +
                "Run PrepareCargoDrySettlementPayment (Phase 4B) first.");

        // ── Mark payout processing via Payment module lifecycle service ────────
        var payoutResult = await _lifecycleService.MarkProcessingAsync(
            payoutRecordId:    settlement.PayoutRecordId.Value,
            processedByUserId: request.ProcessedByUserId,
            externalReference: request.ExternalReference,
            note:              request.Note,
            ct:                ct);

        return new MarkCargoDrySettlementPayoutProcessingResponse
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
