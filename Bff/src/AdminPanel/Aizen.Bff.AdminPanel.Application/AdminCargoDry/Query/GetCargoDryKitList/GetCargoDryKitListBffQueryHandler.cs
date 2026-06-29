using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryKitList;

[DocumentationInfo("Get CargoDry kit list BFF query handler", "Calls the CargoDry admin kit list endpoint with filter/pagination parameters.")]
public sealed class GetCargoDryKitListBffQueryHandler
    : AizenQueryHandler<GetCargoDryKitListBffQuery, GetCargoDryKitListBffResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public GetCargoDryKitListBffQueryHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryKitListBffResponse> Handle(
        GetCargoDryKitListBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetKitsAsync(
            request.Status,
            request.Search,
            request.VesselId,
            request.OwnerUserId,
            request.BatchCode,
            request.Page,
            request.PageSize,
            ct);

        return new GetCargoDryKitListBffResponse { KitList = result };
    }
}
