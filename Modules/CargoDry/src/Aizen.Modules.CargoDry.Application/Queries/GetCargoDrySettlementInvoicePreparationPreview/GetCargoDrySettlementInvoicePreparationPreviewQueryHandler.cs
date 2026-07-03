using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementInvoicePreparationPreview;

[DocumentationInfo("GetCargoDrySettlementInvoicePreparationPreviewQueryHandler",
    "Returns eligibility preview for invoice preparation of a CargoDry sell-through settlement. " +
    "Checks: settlement found, status = Scheduled, payout record exists (Phase 4B), " +
    "payout amount > 0, invoice not yet prepared (idempotency info). " +
    "Never throws for business ineligibility — surfaces blocking reasons in the response. " +
    "Phase 4C (July 2026).")]
public sealed class GetCargoDrySettlementInvoicePreparationPreviewQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementInvoicePreparationPreviewQuery, GetCargoDrySettlementInvoicePreparationPreviewResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;

    public GetCargoDrySettlementInvoicePreparationPreviewQueryHandler(
        ICargoDrySellThroughSettlementRepository settlements)
        => _settlements = settlements;

    public override async Task<GetCargoDrySettlementInvoicePreparationPreviewResponse> Handle(
        GetCargoDrySettlementInvoicePreparationPreviewQuery request, CancellationToken ct)
    {
        var settlement = await _settlements.GetByIdAsync(request.SettlementId, ct);

        // Settlement not found — return clear "not found" response without throwing
        if (settlement is null)
        {
            return new GetCargoDrySettlementInvoicePreparationPreviewResponse
            {
                SettlementId       = request.SettlementId,
                SettlementCode     = "UNKNOWN",
                Status             = default,
                StatusName         = "NotFound",
                CanPrepare         = false,
                BlockingReasons    = ["Settlement not found."],
                RecommendedActions = ["Verify the settlement ID is correct."],
            };
        }

        var blockingReasons    = new List<string>();
        var recommendedActions = new List<string>();

        // ── Check: already prepared (idempotency info) ─────────────────────────
        bool alreadyPrepared = settlement.InvoiceId.HasValue;

        // ── Check: status must be Scheduled ───────────────────────────────────
        if (settlement.Status != CargoDrySellThroughSettlementStatus.Scheduled && !alreadyPrepared)
        {
            blockingReasons.Add(
                $"Settlement is in {settlement.Status} status. " +
                "Only Scheduled settlements can have invoice prepared.");

            if (settlement.Status == CargoDrySellThroughSettlementStatus.Pending)
                recommendedActions.Add(
                    "Run ResolveMonthlySellThroughSettlement, then PrepareCargoDrySettlementPayment " +
                    "to move the settlement to Scheduled status.");
            else if (settlement.Status == CargoDrySellThroughSettlementStatus.ReadyForSettlement)
                recommendedActions.Add(
                    "Run PrepareCargoDrySettlementPayment (Phase 4B) first to create the " +
                    "payout record and move settlement to Scheduled status.");
            else if (settlement.Status == CargoDrySellThroughSettlementStatus.Settled)
                recommendedActions.Add("Settlement is already fully settled — invoice should already exist.");
            else if (settlement.Status == CargoDrySellThroughSettlementStatus.Cancelled)
                recommendedActions.Add("Settlement has been cancelled — cannot prepare invoice.");
            else if (settlement.Status == CargoDrySellThroughSettlementStatus.Disputed)
                recommendedActions.Add("Resolve the dispute before preparing invoice.");
        }

        // ── Check: payout record must exist (Phase 4B must be complete) ────────
        if (!settlement.PayoutRecordId.HasValue && !alreadyPrepared)
        {
            blockingReasons.Add(
                "No payout record found for this settlement. " +
                "PrepareCargoDrySettlementPayment (Phase 4B) must be run before preparing the invoice.");
            recommendedActions.Add(
                "Run PrepareCargoDrySettlementPayment endpoint first to create the PayoutRecord " +
                "and transition settlement to Scheduled status.");
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

        bool canPrepare = blockingReasons.Count == 0 || alreadyPrepared;

        return new GetCargoDrySettlementInvoicePreparationPreviewResponse
        {
            SettlementId             = settlement.Id,
            SettlementCode           = settlement.SettlementCode,
            Status                   = settlement.Status,
            StatusName               = settlement.Status.ToString(),
            CanPrepare               = canPrepare,
            BlockingReasons          = blockingReasons,
            RecommendedActions       = recommendedActions,
            ProviderPayoutAmount     = settlement.ProviderPayoutAmount,
            TotalSaleAmount          = settlement.TotalSaleAmount,
            TotalCommissionAmount    = settlement.TotalCommissionAmount,
            TotalKitCount            = settlement.TotalKitCount,
            CurrencyCode             = settlement.CurrencyCode,
            ProductCode              = settlement.ProductCode,
            PeriodStartUtc           = settlement.PeriodStartUtc,
            PeriodEndUtc             = settlement.PeriodEndUtc,
            PayoutRecordId           = settlement.PayoutRecordId,
            PaymentPreparedAtUtc     = settlement.PaymentPreparedAtUtc,
            InvoiceExists            = alreadyPrepared,
            ExistingInvoiceId        = settlement.InvoiceId,
            InvoicePreparedAtUtc     = settlement.InvoicePreparedAtUtc,
            InvoicePreparedByUserId  = settlement.InvoicePreparedByUserId,
        };
    }
}
