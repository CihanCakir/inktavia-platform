using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Interface;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementPayoutExecutionPreview;

[DocumentationInfo("GetCargoDrySettlementPayoutExecutionPreviewQueryHandler",
    "Loads the settlement and its linked payout record state to produce an eligibility preview " +
    "for all Phase 4D lifecycle actions: Approve, MarkProcessing, Complete, Fail. " +
    "Delegates to ICargoDrySettlementPayoutLifecycleService.GetPayoutStateAsync() when a payout record exists. " +
    "Never throws for business ineligibility — surfaces CanXxx flags and BlockingReasons. " +
    "Phase 4D (July 2026).")]
public sealed class GetCargoDrySettlementPayoutExecutionPreviewQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementPayoutExecutionPreviewQuery, GetCargoDrySettlementPayoutExecutionPreviewResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;
    private readonly ICargoDrySettlementPayoutLifecycleService _lifecycleService;

    public GetCargoDrySettlementPayoutExecutionPreviewQueryHandler(
        ICargoDrySellThroughSettlementRepository settlements,
        ICargoDrySettlementPayoutLifecycleService lifecycleService)
    {
        _settlements      = settlements;
        _lifecycleService = lifecycleService;
    }

    public override async Task<GetCargoDrySettlementPayoutExecutionPreviewResponse> Handle(
        GetCargoDrySettlementPayoutExecutionPreviewQuery request, CancellationToken ct)
    {
        var settlement = await _settlements.GetByIdAsync(request.SettlementId, ct);

        if (settlement is null)
        {
            return new GetCargoDrySettlementPayoutExecutionPreviewResponse
            {
                SettlementId    = request.SettlementId,
                SettlementCode  = "UNKNOWN",
                Status          = default,
                StatusName      = "NotFound",
                BlockingReasons = ["Settlement not found."],
                RecommendedActions = ["Verify the settlement ID is correct."],
            };
        }

        var blockingReasons    = new List<string>();
        var recommendedActions = new List<string>();

        bool paymentPrepared = settlement.PayoutRecordId.HasValue;
        bool invoicePrepared = settlement.InvoiceId.HasValue;
        bool isScheduled     = settlement.Status == CargoDrySellThroughSettlementStatus.Scheduled;
        bool isSettled       = settlement.Status == CargoDrySellThroughSettlementStatus.Settled;

        // ── Collect global blocking reasons ───────────────────────────────────
        if (!isScheduled && !isSettled)
        {
            blockingReasons.Add(
                $"Settlement is in {settlement.Status} status. " +
                "Payout execution requires Scheduled status.");

            if (settlement.Status == CargoDrySellThroughSettlementStatus.ReadyForSettlement)
                recommendedActions.Add("Run PrepareCargoDrySettlementPayment (Phase 4B) to transition to Scheduled.");
            else if (settlement.Status == CargoDrySellThroughSettlementStatus.Pending)
                recommendedActions.Add(
                    "Run ResolveMonthlySellThroughSettlement then PrepareCargoDrySettlementPayment (Phase 4B).");
        }

        if (!paymentPrepared)
        {
            blockingReasons.Add("No PayoutRecord found. Phase 4B (PrepareCargoDrySettlementPayment) must be completed first.");
            recommendedActions.Add("Run POST .../prepare-payment to create the PayoutRecord.");
        }

        if (!invoicePrepared)
        {
            blockingReasons.Add("No invoice found. Phase 4C (PrepareCargoDrySettlementInvoice) must be completed before completing the payout.");
            recommendedActions.Add("Run POST .../prepare-invoice to create the ProviderSettlementStatement before completing the payout.");
        }

        // ── Load payout state from Payment module ─────────────────────────────
        Aizen.Modules.Payment.Abstraction.Model.Result.CargoDryPayoutLifecycleResultDto? payoutState = null;

        if (paymentPrepared)
        {
            try
            {
                payoutState = await _lifecycleService.GetPayoutStateAsync(
                    settlement.PayoutRecordId!.Value,
                    settlement.Id,
                    ct);
            }
            catch
            {
                // Payout record may have been deleted or is inaccessible — treat as missing
                blockingReasons.Add("Payout record could not be loaded from the Payment module.");
                recommendedActions.Add("Contact a system administrator to verify the payout record integrity.");
            }
        }

        // ── Compute lifecycle action eligibility ──────────────────────────────
        var ps = payoutState?.PayoutStatus;

        bool canApprovePayout  = isScheduled && paymentPrepared && ps == PayoutStatus.Pending;
        bool canMarkProcessing = isScheduled && paymentPrepared
                                 && (ps == PayoutStatus.Approved || ps == PayoutStatus.Pending);
        bool canCompletePayout = isScheduled && paymentPrepared && invoicePrepared
                                 && (ps == PayoutStatus.Approved || ps == PayoutStatus.Processing || ps == PayoutStatus.Pending);
        bool canFailPayout     = isScheduled && paymentPrepared
                                 && ps is not (null or PayoutStatus.Completed or PayoutStatus.Cancelled);

        return new GetCargoDrySettlementPayoutExecutionPreviewResponse
        {
            SettlementId            = settlement.Id,
            SettlementCode          = settlement.SettlementCode,
            Status                  = settlement.Status,
            StatusName              = settlement.Status.ToString(),
            ProviderPayoutAmount    = settlement.ProviderPayoutAmount,
            CurrencyCode            = settlement.CurrencyCode,
            ProductCode             = settlement.ProductCode,
            PeriodStartUtc          = settlement.PeriodStartUtc,
            PeriodEndUtc            = settlement.PeriodEndUtc,
            // Phase prerequisite state
            PaymentPrepared         = paymentPrepared,
            PayoutRecordId          = settlement.PayoutRecordId,
            PaymentPreparedAtUtc    = settlement.PaymentPreparedAtUtc,
            InvoicePrepared         = invoicePrepared,
            InvoiceId               = settlement.InvoiceId,
            InvoicePreparedAtUtc    = settlement.InvoicePreparedAtUtc,
            // Payout state from Payment module
            PayoutStatus            = payoutState?.PayoutStatus,
            PayoutStatusName        = payoutState?.PayoutStatus.ToString(),
            ExternalReference       = payoutState?.ExternalReference,
            PayoutApprovedAtUtc     = payoutState?.ApprovedAtUtc,
            PayoutProcessingAtUtc   = payoutState?.ProcessingAtUtc,
            PayoutCompletedAtUtc    = payoutState?.CompletedAtUtc,
            PayoutFailedAtUtc       = payoutState?.FailedAtUtc,
            PayoutFailureReason     = payoutState?.FailureReason ?? settlement.PayoutFailureReason,
            // CargoDry-side closure
            PayoutCompletionReference = settlement.PayoutCompletionReference,
            PayoutLifecycleNote       = settlement.PayoutLifecycleNote,
            // Eligibility flags
            CanApprovePayout        = canApprovePayout,
            CanMarkProcessing       = canMarkProcessing,
            CanCompletePayout       = canCompletePayout,
            CanFailPayout           = canFailPayout,
            BlockingReasons         = blockingReasons,
            RecommendedActions      = recommendedActions,
        };
    }
}
