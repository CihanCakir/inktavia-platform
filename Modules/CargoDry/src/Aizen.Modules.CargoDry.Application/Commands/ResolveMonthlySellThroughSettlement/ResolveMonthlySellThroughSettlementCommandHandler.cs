using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.ResolveMonthlySellThroughSettlement;

[DocumentationInfo("Resolve monthly sell-through settlement command handler",
    "Verifies that all attributions in the settlement are financially resolved, " +
    "recalculates settlement totals from resolved amounts, " +
    "and marks the settlement as ReadyForSettlement. " +
    "Blocks with a business error if any attribution is unresolved. " +
    "Phase 4A (July 2026).")]
public sealed class ResolveMonthlySellThroughSettlementCommandHandler
    : AizenCommandHandler<ResolveMonthlySellThroughSettlementCommand, ResolveMonthlySellThroughSettlementResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;
    private readonly ICargoDrySalesAttributionRepository      _attributions;

    public ResolveMonthlySellThroughSettlementCommandHandler(
        ICargoDrySellThroughSettlementRepository settlements,
        ICargoDrySalesAttributionRepository      attributions)
    {
        _settlements  = settlements;
        _attributions = attributions;
    }

    public override async Task<ResolveMonthlySellThroughSettlementResponse> Handle(
        ResolveMonthlySellThroughSettlementCommand request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        // ── Load settlement ────────────────────────────────────────────────────
        var settlement = await _settlements.GetByIdAsync(request.SettlementId, ct)
            ?? throw new AizenBusinessException(
                $"Sell-through settlement {request.SettlementId} not found.");

        // ── Status gate (Task 2 repair path) ───────────────────────────────────
        // Pending is the normal case. A ReadyForSettlement settlement may also be re-resolved (self-heal for a settlement
        // whose totals were wiped because it was resolved before its attributions were linked) — but ONLY while no
        // payment/invoice has been prepared, so committed downstream amounts can never change silently. Reopen it to
        // Pending first so the same recalc + MarkReadyForSettlement flow applies.
        if (settlement.Status == CargoDrySellThroughSettlementStatus.ReadyForSettlement)
        {
            if (settlement.PayoutRecordId.HasValue || settlement.InvoiceId.HasValue)
                throw new AizenBusinessException(
                    $"Settlement {settlement.Id} ({settlement.SettlementCode}) is ReadyForSettlement with a prepared " +
                    $"payment/invoice — it cannot be re-resolved. Handle via the payout/dispute flow instead.");

            settlement.ReopenForResolution();
        }
        else if (settlement.Status != CargoDrySellThroughSettlementStatus.Pending)
        {
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) is in status " +
                $"{settlement.Status} — only Pending (or an unpaid ReadyForSettlement) settlement can be resolved.");
        }

        // ── Verify all attributions are financially resolved ───────────────────
        var unresolved = await _attributions.GetUnresolvedBySettlementIdAsync(settlement.Id, ct);
        if (unresolved.Count > 0)
        {
            var unresolvedIds = string.Join(", ", unresolved.Select(a => a.Id));
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) has " +
                $"{unresolved.Count} attribution(s) without resolved financials: [{unresolvedIds}]. " +
                "All attributions must be financially resolved before the settlement can be finalized.");
        }

        // ── Recalculate totals from all resolved attributions ──────────────────
        var allAttributions = await _attributions.GetBySettlementIdAsync(settlement.Id, ct);

        // Task 3 — guard the wipe: 0 linked attributions but the settlement still records kits means the settlement↔
        // attribution link is broken (the dead-link bug). Zeroing the totals would silently hide the corruption and
        // prepare a ₺0 payout. Refuse instead so the orphans can be linked (activation fix / second-pass) and retried.
        if (allAttributions.Count == 0 && settlement.TotalKitCount > 0)
            throw new AizenBusinessException(
                $"Settlement {settlement.Id} ({settlement.SettlementCode}) has 0 linked attributions but records " +
                $"{settlement.TotalKitCount} kit(s). Attribution linking is broken — refusing to zero the totals. " +
                "Run the settlement automation (second-pass linker) or verify SellThroughSettlementId links, then retry.");

        var totalKitCount            = allAttributions.Count;
        var totalSaleAmount          = Math.Round(allAttributions.Sum(a => a.SalePrice            ?? 0m), 2, MidpointRounding.AwayFromZero);
        var totalProviderShareAmount = Math.Round(allAttributions.Sum(a => a.ProviderShareAmount ?? 0m), 2, MidpointRounding.AwayFromZero);

        settlement.RecalculateTotals(
            totalKitCount:            totalKitCount,
            totalSaleAmount:          totalSaleAmount,
            totalProviderShareAmount: totalProviderShareAmount);

        // ── Mark settlement ReadyForSettlement ─────────────────────────────────
        settlement.MarkReadyForSettlement(
            readyAtUtc: nowUtc,
            note:       request.ResolutionNote);

        await _settlements.SaveChangesAsync(ct);

        // ── Map to DTO ─────────────────────────────────────────────────────────
        return new ResolveMonthlySellThroughSettlementResponse
        {
            Settlement = new CargoDrySellThroughSettlementDto
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
                CreatedAtUtc            = settlement.CreatedAtUtc,
            }
        };
    }
}
