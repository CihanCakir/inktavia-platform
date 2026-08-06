using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySalesAttributionsPaged;

[DocumentationInfo("Get CargoDry sales attributions paged BFF query handler",
    "Returns a filtered and paginated list of sales attribution records from the CargoDry commercial admin endpoint. " +
    "Phase 3 (July 2026): Sales Attribution & Sell-Through Settlement Foundation.")]
public sealed class GetCargoDrySalesAttributionsPagedBffQueryHandler
    : AizenQueryHandler<GetCargoDrySalesAttributionsPagedBffQuery, GetCargoDrySalesAttributionsPagedBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDrySalesAttributionsPagedBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySalesAttributionsPagedBffResponse> Handle(
        GetCargoDrySalesAttributionsPagedBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetSalesAttributionsPagedAsync(
            request.ProviderProfileId,
            request.ProductCode,
            request.BatchCode,
            request.SalesChannel,
            request.CommercialModel,
            request.Status,
            request.SellThroughSettlementId,
            request.DateFrom,
            request.DateTo,
            request.Search,
            request.Page,
            request.PageSize,
            ct);

        return new GetCargoDrySalesAttributionsPagedBffResponse { PagedResult = result };
    }
}
