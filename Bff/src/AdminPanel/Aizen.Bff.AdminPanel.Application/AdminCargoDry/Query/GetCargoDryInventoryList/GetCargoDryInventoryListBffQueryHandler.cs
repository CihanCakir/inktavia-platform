using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryInventoryList;

[DocumentationInfo("Get CargoDry inventory list BFF query handler",
    "Passes paged inventory list request to the CargoDry admin inventory endpoint. Phase 2.")]
public sealed class GetCargoDryInventoryListBffQueryHandler
    : AizenQueryHandler<GetCargoDryInventoryListBffQuery, GetCargoDryInventoryListBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryInventoryListBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryInventoryListBffResponse> Handle(
        GetCargoDryInventoryListBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetInventoryListAsync(
            request.ProviderProfileId,
            request.ProductCode,
            request.CommercialModel,
            request.SalesChannel,
            request.HasAvailableStock,
            request.Search,
            request.Page,
            request.PageSize,
            ct);

        return new GetCargoDryInventoryListBffResponse { PagedResult = result };
    }
}
