using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.ResolveCargoDrySalesAttributionFinancials;

[DocumentationInfo("Resolve CargoDry sales attribution financials command handler",
    "Resolves financial amounts on a sales attribution record via the CargoDry commercial rule resolver. " +
    "The resolver evaluates a 7-tier cascade: AdminOverride → provider CommissionRule → product+channel CommissionRule " +
    "→ ConsignmentAgreement.ConsignmentRate → product.ProviderCommissionRate → Unresolved. " +
    "After successful resolution the rule trace is recorded on the attribution. " +
    "Parent sell-through settlement totals are recalculated when applicable. " +
    "Phase 5 (July 2026): replaced inline cascade with ICargoDryCommercialRuleResolver.")]
public sealed class ResolveCargoDrySalesAttributionFinancialsCommandHandler
    : AizenCommandHandler<ResolveCargoDrySalesAttributionFinancialsCommand, ResolveCargoDrySalesAttributionFinancialsResponse>
{
    private readonly ICargoDrySalesAttributionRepository      _attributions;
    private readonly ICargoDrySellThroughSettlementRepository _settlements;
    private readonly ICargoDryCommercialRuleResolver          _resolver;

    public ResolveCargoDrySalesAttributionFinancialsCommandHandler(
        ICargoDrySalesAttributionRepository      attributions,
        ICargoDrySellThroughSettlementRepository settlements,
        ICargoDryCommercialRuleResolver          resolver)
    {
        _attributions = attributions;
        _settlements  = settlements;
        _resolver     = resolver;
    }

    public override async Task<ResolveCargoDrySalesAttributionFinancialsResponse> Handle(
        ResolveCargoDrySalesAttributionFinancialsCommand request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        // ── Load attribution ───────────────────────────────────────────────────
        var attribution = await _attributions.GetByIdAsync(request.SalesAttributionId, ct)
            ?? throw new AizenBusinessException(
                $"Sales attribution {request.SalesAttributionId} not found.");

        if (attribution.Status is CargoDrySalesAttributionStatus.Settled
                                or CargoDrySalesAttributionStatus.Cancelled)
            throw new AizenBusinessException(
                $"Cannot resolve financials on attribution {attribution.Id} with status {attribution.Status}. " +
                "Only Pending, Attributed, SettlementPending, and CommercialReviewRequired attributions can be resolved.");

        // ── Run commercial rule resolver ───────────────────────────────────────
        var resolutionRequest = new CargoDryCommercialRuleResolutionRequest
        {
            ProviderProfileId      = attribution.ProviderProfileId,
            ProductCode            = attribution.ProductCode,
            SalesChannel           = attribution.SalesChannel,
            CommercialModel        = attribution.CommercialModel,
            CurrencyCode           = request.CurrencyCode,
            EffectiveAtUtc         = nowUtc,
            SalePrice              = request.SalePrice,
            ConsignmentAgreementId = attribution.ConsignmentAgreementId,
            AdminOverrideRate      = request.CommissionRateOverride,
            RequestedByUserId      = request.ResolvedByUserId,
        };

        var resolution = await _resolver.ResolveAsync(resolutionRequest, ct);

        if (!resolution.CanResolve)
        {
            var reasons = string.Join(" | ", resolution.BlockingReasons);
            throw new AizenBusinessException(
                $"Cannot resolve commission rate for attribution {attribution.Id}. " +
                $"Reasons: {reasons}");
        }

        var resolvedRate = resolution.ResolvedRate!.Value;

        // ── Record rule trace on attribution ───────────────────────────────────
        attribution.RecordRuleTrace(
            ruleSource:         resolution.RuleSource!,
            resolvedRate:       resolvedRate,
            resolvedAtUtc:      nowUtc,
            resolvedByUserId:   request.ResolvedByUserId,
            ruleId:             resolution.RuleId,
            ruleName:           resolution.RuleName,
            ruleResolutionNote: request.ResolutionNote);

        // ── Apply financial resolution ─────────────────────────────────────────
        attribution.ResolveFinancials(
            salePrice:        request.SalePrice,
            commissionRate:   resolvedRate,
            currencyCode:     request.CurrencyCode,
            resolvedAtUtc:    nowUtc,
            resolvedByUserId: request.ResolvedByUserId,
            resolutionNote:   request.ResolutionNote);

        // ── Recalculate parent settlement totals ───────────────────────────────
        if (attribution.SellThroughSettlementId.HasValue)
        {
            var settlement = await _settlements.GetByIdAsync(attribution.SellThroughSettlementId.Value, ct);
            if (settlement is not null)
            {
                var allAttributions = await _attributions.GetBySettlementIdAsync(settlement.Id, ct);
                var resolvedOnes    = allAttributions.Where(a => a.IsFinanciallyResolved).ToList();

                settlement.RecalculateTotals(
                    totalKitCount:            allAttributions.Count,
                    totalSaleAmount:          resolvedOnes.Sum(a => a.SalePrice ?? 0m),
                    totalProviderShareAmount: resolvedOnes.Sum(a => a.ProviderShareAmount ?? 0m));
            }
        }

        await _attributions.SaveChangesAsync(ct);

        // ── Map to DTO ─────────────────────────────────────────────────────────
        return new ResolveCargoDrySalesAttributionFinancialsResponse
        {
            Attribution = new CargoDrySalesAttributionDto
            {
                Id                        = attribution.Id,
                PublicId                  = attribution.PublicId,
                KitId                     = attribution.KitId,
                SerialNumber              = attribution.SerialNumber,
                KitCode                   = attribution.KitCode,
                ProductCode               = attribution.ProductCode,
                BatchCode                 = attribution.BatchCode,
                ProviderProfileId         = attribution.ProviderProfileId,
                SalesChannel              = attribution.SalesChannel,
                SalesChannelName          = attribution.SalesChannel.ToString(),
                CommercialModel           = attribution.CommercialModel,
                CommercialModelName       = attribution.CommercialModel.ToString(),
                ConsignmentAgreementId    = attribution.ConsignmentAgreementId,
                InventoryId               = attribution.InventoryId,
                Status                    = attribution.Status,
                StatusName                = attribution.Status.ToString(),
                SalePrice                 = attribution.SalePrice,
                CommissionRate            = attribution.CommissionRate,
                CommissionAmount          = attribution.CommissionAmount,
                CurrencyCode              = attribution.CurrencyCode,
                ProviderShareAmount       = attribution.ProviderShareAmount,
                PlatformShareAmount       = attribution.PlatformShareAmount,
                IsFinanciallyResolved     = attribution.IsFinanciallyResolved,
                FinancialResolvedAtUtc    = attribution.FinancialResolvedAtUtc,
                FinancialResolvedByUserId = attribution.FinancialResolvedByUserId,
                ResolutionNote            = attribution.ResolutionNote,
                SellThroughSettlementId   = attribution.SellThroughSettlementId,
                // Phase 5: rule trace
                ResolvedRuleId            = attribution.ResolvedRuleId,
                ResolvedRuleSource        = attribution.ResolvedRuleSource,
                ResolvedRuleName          = attribution.ResolvedRuleName,
                ResolvedRate              = attribution.ResolvedRate,
                RateResolvedAtUtc         = attribution.RateResolvedAtUtc,
                RateResolvedByUserId      = attribution.RateResolvedByUserId,
                RuleResolutionNote        = attribution.RuleResolutionNote,
                AttributedAt              = attribution.AttributedAt,
                AttributedByUserId        = attribution.AttributedByUserId,
                ReviewNote                = attribution.ReviewNote,
                ReviewedByUserId          = attribution.ReviewedByUserId,
                ReviewedAt                = attribution.ReviewedAt,
                CreatedAtUtc              = attribution.CreatedAtUtc,
            }
        };
    }
}
