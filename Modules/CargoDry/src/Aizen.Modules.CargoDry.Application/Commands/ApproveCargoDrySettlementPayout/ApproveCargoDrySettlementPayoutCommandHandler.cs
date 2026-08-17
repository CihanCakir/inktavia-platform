using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.Payment.Abstraction.Interface;

namespace Aizen.Modules.CargoDry.Application.Commands.ApproveCargoDrySettlementPayout;

[DocumentationInfo("ApproveCargoDrySettlementPayoutCommandHandler",
    "Validates the settlement is in Scheduled status with an existing PayoutRecord, " +
    "then calls ICargoDrySettlementPayoutLifecycleService.ApproveAsync() to transition " +
    "the PayoutRecord from Pending → Approved. " +
    "Settlement status remains Scheduled — only CompleteCargoDrySettlementPayoutCommand " +
    "can advance the settlement to Settled. " +
    "No Iyzico call. No bank transfer. Phase 4D (July 2026).")]
public sealed class ApproveCargoDrySettlementPayoutCommandHandler
    : AizenCommandHandler<ApproveCargoDrySettlementPayoutCommand, ApproveCargoDrySettlementPayoutResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;
    private readonly ICargoDrySettlementPayoutLifecycleService _lifecycleService;

    public ApproveCargoDrySettlementPayoutCommandHandler(
        ICargoDrySellThroughSettlementRepository settlements,
        ICargoDrySettlementPayoutLifecycleService lifecycleService)
    {
        _settlements      = settlements;
        _lifecycleService = lifecycleService;
    }

    public override async Task<ApproveCargoDrySettlementPayoutResponse> Handle(
        ApproveCargoDrySettlementPayoutCommand request, CancellationToken ct)
    {
        // ── Load settlement ────────────────────────────────────────────────────
        var settlement = await _settlements.GetByIdAsync(request.SettlementId, ct)
            ?? throw new AizenBusinessException(
                $"Sell-through settlement {request.SettlementId} not found.");

        // ── Status guard: must be Scheduled ───────────────────────────────────
        if (settlement.Status != CargoDrySellThroughSettlementStatus.Scheduled)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) is in {settlement.Status} status. " +
                "Only Scheduled settlements can have payout approved.");

        // ── Payout record must exist (Phase 4B must be complete) ──────────────
        if (!settlement.PayoutRecordId.HasValue)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) has no linked PayoutRecord. " +
                "Run PrepareCargoDrySettlementPayment (Phase 4B) first.");

        // ── Approve payout via Payment module lifecycle service ────────────────
        var payoutResult = await _lifecycleService.ApproveAsync(
            payoutRecordId:   settlement.PayoutRecordId.Value,
            approvedByUserId: request.ApprovedByUserId,
            note:             request.Note,
            ct:               ct);

        return new ApproveCargoDrySettlementPayoutResponse
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
