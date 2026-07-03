using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySalesAttributionDetail;

[DocumentationInfo("Get CargoDry sales attribution detail BFF query handler",
    "Returns the full detail of a single sales attribution record by Id. " +
    "Phase 3 (July 2026): Sales Attribution & Sell-Through Settlement Foundation.")]
public sealed class GetCargoDrySalesAttributionDetailBffQueryHandler
    : AizenQueryHandler<GetCargoDrySalesAttributionDetailBffQuery, GetCargoDrySalesAttributionDetailBffResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public GetCargoDrySalesAttributionDetailBffQueryHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDrySalesAttributionDetailBffResponse> Handle(
        GetCargoDrySalesAttributionDetailBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetSalesAttributionDetailAsync(request.Id, ct);
        return new GetCargoDrySalesAttributionDetailBffResponse { Detail = result };
    }
}
