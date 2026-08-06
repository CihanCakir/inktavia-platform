using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySalesAttributionRuleResolutionPreview;

[DocumentationInfo("GetCargoDrySalesAttributionRuleResolutionPreviewBffQueryHandler",
    "Proxies the attribution-specific rule-resolution-preview call to the CargoDry module. " +
    "The module loads the attribution record and runs the 7-tier resolver against its context, " +
    "applying any salePrice / currencyCode / adminOverrideRate passed as preview overrides. " +
    "Returns the current attribution DTO plus the full resolution result. " +
    "Never persists anything. Safe to call at any time — never throws for business ineligibility. " +
    "Phase 5 (July 2026).")]
public sealed class GetCargoDrySalesAttributionRuleResolutionPreviewBffQueryHandler
    : AizenQueryHandler<GetCargoDrySalesAttributionRuleResolutionPreviewBffQuery,
                        GetCargoDrySalesAttributionRuleResolutionPreviewBffQueryResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDrySalesAttributionRuleResolutionPreviewBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySalesAttributionRuleResolutionPreviewBffQueryResponse> Handle(
        GetCargoDrySalesAttributionRuleResolutionPreviewBffQuery request, CancellationToken ct)
    {
        var preview = await _remote.GetAttributionRuleResolutionPreviewAsync(
            id:               request.SalesAttributionId,
            salePrice:        request.SalePrice,
            currencyCode:     request.CurrencyCode,
            adminOverrideRate: request.AdminOverrideRate,
            ct:               ct);

        return new GetCargoDrySalesAttributionRuleResolutionPreviewBffQueryResponse { Preview = preview };
    }
}
