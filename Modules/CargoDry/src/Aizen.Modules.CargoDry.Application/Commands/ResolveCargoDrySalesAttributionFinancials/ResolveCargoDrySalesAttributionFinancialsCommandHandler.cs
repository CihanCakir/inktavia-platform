using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.ResolveCargoDrySalesAttributionFinancials;

[DocumentationInfo("Resolve CargoDry sales attribution financials command handler",
    "Resolves financial amounts on a sales attribution record. " +
    "Cascades commission rate lookup: override > agreement ConsignmentRate > product.ProviderCommissionRate > 0. " +
    "Recalculates parent sell-through settlement totals if the attribution is linked to one. " +
    "Phase 4A (July 2026).")]
public sealed class ResolveCargoDrySalesAttributionFinancialsCommandHandler
    : AizenCommandHandler<ResolveCargoDrySalesAttributionFinancialsCommand, ResolveCargoDrySalesAttributionFinancialsResponse>
{
    private readonly ICargoDrySalesAttributionRepository      _attributions;
    private readonly ICargoDrySellThroughSettlementRepository _settlements;
    private readonly ICargoDryConsignmentAgreementRepository  _agreements;
    private readonly ICargoDryProductRepository               _products;

    public ResolveCargoDrySalesAttributionFinancialsCommandHandler(
        ICargoDrySalesAttributionRepository      attributions,
        ICargoDrySellThroughSettlementRepository settlements,
        ICargoDryConsignmentAgreementRepository  agreements,
        ICargoDryProductRepository               products)
    {
        _attributions = attributions;
        _settlements  = settlements;
        _agreements   = agreements;
        _products     = products;
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

        // ── Resolve commission rate (cascade) ──────────────────────────────────
        decimal commissionRate;

        if (request.CommissionRateOverride.HasValue)
        {
            // 1. Explicit override — highest priority.
            commissionRate = request.CommissionRateOverride.Value;
        }
        else if (attribution.ConsignmentAgreementId.HasValue)
        {
            // 2. ConsignmentRate from the linked agreement.
            var agreement = await _agreements.GetByIdAsync(attribution.ConsignmentAgreementId.Value, ct);
            commissionRate = agreement?.ConsignmentRate
                ?? throw new AizenBusinessException(
                    $"Consignment agreement {attribution.ConsignmentAgreementId.Value} not found. " +
                    "Provide a CommissionRateOverride or fix the agreement reference.");
        }
        else
        {
            // 3. ProviderCommissionRate from the product catalog.
            var product = await _products.GetByCodeAsync(attribution.ProductCode, ct);
            if (product?.ProviderCommissionRate.HasValue == true)
                commissionRate = product.ProviderCommissionRate!.Value;
            else
                // 4. DirectSale default — platform retains the full SalePrice.
                commissionRate = 0m;
        }

        // ── Apply financial resolution ─────────────────────────────────────────
        attribution.ResolveFinancials(
            salePrice:       request.SalePrice,
            commissionRate:  commissionRate,
            currencyCode:    request.CurrencyCode,
            resolvedAtUtc:   nowUtc,
            resolvedByUserId: request.ResolvedByUserId,
            resolutionNote:  request.ResolutionNote);

        // ── Recalculate parent settlement totals ───────────────────────────────
        if (attribution.SellThroughSettlementId.HasValue)
        {
            var settlement = await _settlements.GetByIdAsync(attribution.SellThroughSettlementId.Value, ct);
            if (settlement is not null)
            {
                var allAttributions = await _attributions.GetBySettlementIdAsync(settlement.Id, ct);

                var resolvedOnes = allAttributions.Where(a => a.IsFinanciallyResolved).ToList();

                settlement.RecalculateTotals(
                    totalKitCount:             allAttributions.Count,
                    totalSaleAmount:           resolvedOnes.Sum(a => a.SalePrice ?? 0m),
                    totalProviderShareAmount:  resolvedOnes.Sum(a => a.ProviderShareAmount ?? 0m));
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
