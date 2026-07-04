using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySalesAttributionRuleResolutionPreview;

[DocumentationInfo("GetCargoDrySalesAttributionRuleResolutionPreviewQueryHandler",
    "Loads an existing sales attribution record and runs the commercial rule resolver against it " +
    "in read-only mode. Returns the current attribution DTO plus the full resolution result. " +
    "Does not persist anything. Phase 5 (July 2026).")]
public sealed class GetCargoDrySalesAttributionRuleResolutionPreviewQueryHandler
    : AizenQueryHandler<GetCargoDrySalesAttributionRuleResolutionPreviewQuery, GetCargoDrySalesAttributionRuleResolutionPreviewResponse>
{
    private readonly ICargoDrySalesAttributionRepository _attributions;
    private readonly ICargoDryCommercialRuleResolver     _resolver;

    public GetCargoDrySalesAttributionRuleResolutionPreviewQueryHandler(
        ICargoDrySalesAttributionRepository attributions,
        ICargoDryCommercialRuleResolver     resolver)
    {
        _attributions = attributions;
        _resolver     = resolver;
    }

    public override async Task<GetCargoDrySalesAttributionRuleResolutionPreviewResponse> Handle(
        GetCargoDrySalesAttributionRuleResolutionPreviewQuery request, CancellationToken ct)
    {
        var attribution = await _attributions.GetByIdAsync(request.SalesAttributionId, ct)
            ?? throw new AizenBusinessException(
                $"Sales attribution {request.SalesAttributionId} not found.");

        var salePrice    = request.SalePrice    ?? attribution.SalePrice;
        var currencyCode = request.CurrencyCode ?? attribution.CurrencyCode ?? "TRY";

        var resolutionRequest = new CargoDryCommercialRuleResolutionRequest
        {
            ProviderProfileId      = attribution.ProviderProfileId,
            ProductCode            = attribution.ProductCode,
            SalesChannel           = attribution.SalesChannel,
            CommercialModel        = attribution.CommercialModel,
            CurrencyCode           = currencyCode,
            EffectiveAtUtc         = DateTime.UtcNow,
            SalePrice              = salePrice,
            ConsignmentAgreementId = attribution.ConsignmentAgreementId,
            AdminOverrideRate      = request.AdminOverrideRate,
            RequestedByUserId      = null, // preview only
        };

        var resolution = await _resolver.ResolveAsync(resolutionRequest, ct);

        // ── Map attribution to DTO ────────────────────────────────────────────────
        var attributionDto = new CargoDrySalesAttributionDto
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
        };

        return new GetCargoDrySalesAttributionRuleResolutionPreviewResponse
        {
            Attribution = attributionDto,
            Resolution  = resolution,
        };
    }
}
