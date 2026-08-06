using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySellThroughSettlementsPaged;

[DocumentationInfo("Get CargoDry sell-through settlements paged BFF query handler",
    "Returns a filtered and paginated list of sell-through settlement records from the CargoDry commercial admin endpoint. " +
    "Phase 3 (July 2026): Sales Attribution & Sell-Through Settlement Foundation.")]
public sealed class GetCargoDrySellThroughSettlementsPagedBffQueryHandler
    : AizenQueryHandler<GetCargoDrySellThroughSettlementsPagedBffQuery, GetCargoDrySellThroughSettlementsPagedBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDrySellThroughSettlementsPagedBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySellThroughSettlementsPagedBffResponse> Handle(
        GetCargoDrySellThroughSettlementsPagedBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetSellThroughSettlementsPagedAsync(
            request.ProviderProfileId,
            request.ConsignmentAgreementId,
            request.ProductCode,
            request.Status,
            request.PeriodFrom,
            request.PeriodTo,
            request.Search,
            request.Page,
            request.PageSize,
            ct);

        return new GetCargoDrySellThroughSettlementsPagedBffResponse { PagedResult = result };
    }
}
