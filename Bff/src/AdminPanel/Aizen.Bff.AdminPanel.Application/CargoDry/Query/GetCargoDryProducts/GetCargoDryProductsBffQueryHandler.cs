using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryProducts;

[DocumentationInfo("Get CargoDry products BFF query handler", "Calls the CargoDry admin products endpoint and returns the active product catalog.")]
public sealed class GetCargoDryProductsBffQueryHandler
    : AizenQueryHandler<GetCargoDryProductsBffQuery, GetCargoDryProductsBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryProductsBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryProductsBffResponse> Handle(
        GetCargoDryProductsBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetProductsAsync(ct);
        return new GetCargoDryProductsBffResponse { Products = result };
    }
}
