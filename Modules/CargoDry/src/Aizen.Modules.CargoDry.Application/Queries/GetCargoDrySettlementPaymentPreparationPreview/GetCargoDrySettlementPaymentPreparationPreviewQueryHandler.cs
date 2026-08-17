using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementPaymentPreparationPreview;

[DocumentationInfo("GetCargoDrySettlementPaymentPreparationPreviewQueryHandler",
    "Returns eligibility preview for payment preparation of a CargoDry sell-through settlement. " +
    "Checks status, attribution resolution, payout amount, and existing payout record link. " +
    "Never throws for business ineligibility — surfaces blocking reasons in the response. " +
    "Phase 4B (July 2026).")]
public sealed class GetCargoDrySettlementPaymentPreparationPreviewQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementPaymentPreparationPreviewQuery, GetCargoDrySettlementPaymentPreparationPreviewResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;
    private readonly ICargoDrySalesAttributionRepository      _attributions;

    public GetCargoDrySettlementPaymentPreparationPreviewQueryHandler(
        ICargoDrySellThroughSettlementRepository settlements,
        ICargoDrySalesAttributionRepository      attributions)
    {
        _settlements  = settlements;
        _attributions = attributions;
    }

    public override async Task<GetCargoDrySettlementPaymentPreparationPreviewResponse> Handle(
        GetCargoDrySettlementPaymentPreparationPreviewQuery request, CancellationToken ct)
    {
        var settlement = await _settlements.GetByIdAsync(request.SettlementId, ct);

        // Settlement not found — return clear "not found" response without throwing
        if (settlement is null)
        {
            return new GetCargoDrySettlementPaymentPreparationPreviewResponse
            {
                SettlementId     = request.SettlementId,
                SettlementCode   = "UNKNOWN",
                Status           = default,
                StatusName       = "NotFound",
                CanPrepare       = false,
                BlockingReasons  = ["Settlement not found."],
                RecommendedActions = ["Verify the settlement ID is correct."],
            };
        }

        var blockingReasons    = new List<string>();
        var recommendedActions = new List<string>();

        // ── Check: already prepared (idempotency info) ─────────────────────────
        bool alreadyPrepared = settlement.PayoutRecordId.HasValue;

        // ── Check: status must be ReadyForSettlement ───────────────────────────
        if (settlement.Status != CargoDrySellThroughSettlementStatus.ReadyForSettlement
            && !alreadyPrepared)
        {
            blockingReasons.Add(
                $"Settlement is in {settlement.Status} status. " +
                "Only ReadyForSettlement settlements can have payment prepared.");

            if (settlement.Status == CargoDrySellThroughSettlementStatus.Pending)
                recommendedActions.Add(
                    "Run ResolveMonthlySellThroughSettlement to verify attributions and " +
                    "mark the settlement ReadyForSettlement.");
            else if (settlement.Status == CargoDrySellThroughSettlementStatus.Scheduled)
                recommendedActions.Add("Settlement is already scheduled — no further payment preparation needed.");
            else if (settlement.Status == CargoDrySellThroughSettlementStatus.Settled)
                recommendedActions.Add("Settlement is already fully settled.");
            else if (settlement.Status == CargoDrySellThroughSettlementStatus.Cancelled)
                recommendedActions.Add("Settlement has been cancelled — cannot prepare payment.");
            else if (settlement.Status == CargoDrySellThroughSettlementStatus.Disputed)
                recommendedActions.Add("Resolve the dispute before preparing payment.");
        }

        // ── Check: payout amount must be positive ──────────────────────────────
        if (settlement.ProviderPayoutAmount <= 0 && !alreadyPrepared)
        {
            blockingReasons.Add(
                $"ProviderPayoutAmount is {settlement.ProviderPayoutAmount} {settlement.CurrencyCode}. " +
                "Amount must be greater than zero.");
            recommendedActions.Add(
                "Verify that attributions have been resolved with correct sale prices and commission rates.");
        }

        // ── Load attribution counts ────────────────────────────────────────────
        var allAttributions    = await _attributions.GetBySettlementIdAsync(settlement.Id, ct);
        var unresolvedList     = await _attributions.GetUnresolvedBySettlementIdAsync(settlement.Id, ct);
        int totalCount         = allAttributions.Count;
        int unresolvedCount    = unresolvedList.Count;
        int resolvedCount      = totalCount - unresolvedCount;

        // ── Check: all attributions must be resolved ───────────────────────────
        if (unresolvedCount > 0 && !alreadyPrepared)
        {
            blockingReasons.Add(
                $"{unresolvedCount} of {totalCount} attribution(s) are not yet financially resolved.");
            recommendedActions.Add(
                "Use ResolveCargoDrySalesAttributionFinancials for each unresolved attribution, " +
                "then run ResolveMonthlySellThroughSettlement.");
        }

        bool canPrepare = blockingReasons.Count == 0 || alreadyPrepared;

        return new GetCargoDrySettlementPaymentPreparationPreviewResponse
        {
            SettlementId              = settlement.Id,
            SettlementCode            = settlement.SettlementCode,
            Status                    = settlement.Status,
            StatusName                = settlement.Status.ToString(),
            CanPrepare                = canPrepare,
            BlockingReasons           = blockingReasons,
            RecommendedActions        = recommendedActions,
            TotalAttributionCount     = totalCount,
            ResolvedAttributionCount  = resolvedCount,
            UnresolvedAttributionCount = unresolvedCount,
            ProviderPayoutAmount      = settlement.ProviderPayoutAmount,
            CurrencyCode              = settlement.CurrencyCode,
            PayoutRecordExists        = alreadyPrepared,
            ExistingPayoutRecordId    = settlement.PayoutRecordId,
            PaymentPreparedAtUtc      = settlement.PaymentPreparedAtUtc,
            PaymentPreparedByUserId   = settlement.PaymentPreparedByUserId,
        };
    }
}
