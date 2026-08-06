using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySellThroughSettlementDetail;

[DocumentationInfo("Get CargoDry sell-through settlement detail BFF query handler",
    "Returns the full detail of a single sell-through settlement record by Id. " +
    "Phase 3 (July 2026): Sales Attribution & Sell-Through Settlement Foundation.")]
public sealed class GetCargoDrySellThroughSettlementDetailBffQueryHandler
    : AizenQueryHandler<GetCargoDrySellThroughSettlementDetailBffQuery, GetCargoDrySellThroughSettlementDetailBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDrySellThroughSettlementDetailBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySellThroughSettlementDetailBffResponse> Handle(
        GetCargoDrySellThroughSettlementDetailBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetSellThroughSettlementDetailAsync(request.Id, ct);
        return new GetCargoDrySellThroughSettlementDetailBffResponse { Detail = result };
    }
}
