using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryCommercialRuleResolutionPreview;

[DocumentationInfo("GetCargoDryCommercialRuleResolutionPreviewQueryHandler",
    "Runs the CargoDry commercial rule resolver in read-only mode against the supplied inputs. " +
    "Returns the full CargoDryCommercialRuleResolutionResult without persisting anything. " +
    "Useful for admin tooling to preview which rule tier applies before committing. " +
    "Phase 5 (July 2026).")]
public sealed class GetCargoDryCommercialRuleResolutionPreviewQueryHandler
    : AizenQueryHandler<GetCargoDryCommercialRuleResolutionPreviewQuery, GetCargoDryCommercialRuleResolutionPreviewResponse>
{
    private readonly ICargoDryCommercialRuleResolver _resolver;

    public GetCargoDryCommercialRuleResolutionPreviewQueryHandler(
        ICargoDryCommercialRuleResolver resolver)
    {
        _resolver = resolver;
    }

    public override async Task<GetCargoDryCommercialRuleResolutionPreviewResponse> Handle(
        GetCargoDryCommercialRuleResolutionPreviewQuery request, CancellationToken ct)
    {
        var resolutionRequest = new CargoDryCommercialRuleResolutionRequest
        {
            ProviderProfileId      = request.ProviderProfileId,
            ProductCode            = request.ProductCode,
            SalesChannel           = request.SalesChannel,
            CommercialModel        = request.CommercialModel,
            CurrencyCode           = request.CurrencyCode,
            EffectiveAtUtc         = request.EffectiveAtUtc ?? DateTime.UtcNow,
            SalePrice              = request.SalePrice,
            ConsignmentAgreementId = request.ConsignmentAgreementId,
            AdminOverrideRate      = request.AdminOverrideRate,
            RequestedByUserId      = null, // preview only — no user context required
        };

        var resolution = await _resolver.ResolveAsync(resolutionRequest, ct);

        return new GetCargoDryCommercialRuleResolutionPreviewResponse { Resolution = resolution };
    }
}
