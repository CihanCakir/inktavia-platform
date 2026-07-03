using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryInventoryDetail;

[DocumentationInfo("Get CargoDry inventory detail BFF query handler",
    "Returns aggregate inventory summary for a single provider from the CargoDry admin inventory endpoint. Phase 2.")]
public sealed class GetCargoDryInventoryDetailBffQueryHandler
    : AizenQueryHandler<GetCargoDryInventoryDetailBffQuery, GetCargoDryInventoryDetailBffResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public GetCargoDryInventoryDetailBffQueryHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryInventoryDetailBffResponse> Handle(
        GetCargoDryInventoryDetailBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetInventoryDetailAsync(request.ProviderProfileId, ct);

        return new GetCargoDryInventoryDetailBffResponse { Detail = result };
    }
}
