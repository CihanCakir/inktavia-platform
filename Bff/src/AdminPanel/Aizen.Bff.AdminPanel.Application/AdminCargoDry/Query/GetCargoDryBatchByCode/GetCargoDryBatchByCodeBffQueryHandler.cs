using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Refit;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryBatchByCode;

[DocumentationInfo("Get CargoDry batch by code BFF query handler",
    "Calls the CargoDry admin module to resolve a single batch by its code. Returns null when the batch does not exist (404).")]
public sealed class GetCargoDryBatchByCodeBffQueryHandler
    : AizenQueryHandler<GetCargoDryBatchByCodeBffQuery, GetCargoDryBatchByCodeBffResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public GetCargoDryBatchByCodeBffQueryHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryBatchByCodeBffResponse> Handle(
        GetCargoDryBatchByCodeBffQuery request, CancellationToken ct)
    {
        try
        {
            var batch = await _remote.GetBatchByCodeAsync(request.BatchCode, ct);
            return new GetCargoDryBatchByCodeBffResponse { Batch = batch };
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new GetCargoDryBatchByCodeBffResponse { Batch = null };
        }
    }
}
