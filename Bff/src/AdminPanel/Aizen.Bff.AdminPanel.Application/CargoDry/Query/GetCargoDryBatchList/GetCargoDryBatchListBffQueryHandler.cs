using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryBatchList;

[DocumentationInfo("Get CargoDry batch list BFF query handler", "Calls the CargoDry admin batches endpoint and returns paginated batch history.")]
public sealed class GetCargoDryBatchListBffQueryHandler
    : AizenQueryHandler<GetCargoDryBatchListBffQuery, GetCargoDryBatchListBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryBatchListBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryBatchListBffResponse> Handle(
        GetCargoDryBatchListBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetBatchesAsync(request.Page, request.PageSize, ct);
        return new GetCargoDryBatchListBffResponse { BatchList = result };
    }
}
