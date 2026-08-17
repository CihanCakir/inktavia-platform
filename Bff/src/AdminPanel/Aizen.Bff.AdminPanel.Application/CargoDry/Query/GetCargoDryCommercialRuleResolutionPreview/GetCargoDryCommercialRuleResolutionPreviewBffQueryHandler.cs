using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryCommercialRuleResolutionPreview;

[DocumentationInfo("GetCargoDryCommercialRuleResolutionPreviewBffQueryHandler",
    "Proxies the generic commercial rule-resolution-preview call to the CargoDry module. " +
    "Runs the 7-tier resolver (AdminOverride → CommissionRule provider-specific → " +
    "CommissionRule product+channel → ConsignmentAgreement → CargoDryProductDefault → Unresolved) " +
    "in read-only mode. Never throws for business ineligibility — returns CanResolve=false + " +
    "BlockingReasons instead. Phase 5 (July 2026).")]
public sealed class GetCargoDryCommercialRuleResolutionPreviewBffQueryHandler
    : AizenQueryHandler<GetCargoDryCommercialRuleResolutionPreviewBffQuery,
                        GetCargoDryCommercialRuleResolutionPreviewBffQueryResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryCommercialRuleResolutionPreviewBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryCommercialRuleResolutionPreviewBffQueryResponse> Handle(
        GetCargoDryCommercialRuleResolutionPreviewBffQuery request, CancellationToken ct)
    {
        var resolution = await _remote.GetRuleResolutionPreviewAsync(
            productCode:            request.ProductCode,
            salesChannel:           request.SalesChannel,
            commercialModel:        request.CommercialModel,
            currencyCode:           request.CurrencyCode,
            providerProfileId:      request.ProviderProfileId,
            salePrice:              request.SalePrice,
            consignmentAgreementId: request.ConsignmentAgreementId,
            adminOverrideRate:      request.AdminOverrideRate,
            effectiveAtUtc:         request.EffectiveAtUtc,
            ct:                     ct);

        return new GetCargoDryCommercialRuleResolutionPreviewBffQueryResponse { Resolution = resolution };
    }
}
