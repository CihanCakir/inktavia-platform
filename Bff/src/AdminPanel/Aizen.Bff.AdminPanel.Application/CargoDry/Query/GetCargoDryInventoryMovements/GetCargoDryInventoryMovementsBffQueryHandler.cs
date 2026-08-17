using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryInventoryMovements;

[DocumentationInfo("Get CargoDry inventory movements BFF query handler",
    "Passes paged inventory movement request to the CargoDry admin inventory movements endpoint. Phase 2.")]
public sealed class GetCargoDryInventoryMovementsBffQueryHandler
    : AizenQueryHandler<GetCargoDryInventoryMovementsBffQuery, GetCargoDryInventoryMovementsBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryInventoryMovementsBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryInventoryMovementsBffResponse> Handle(
        GetCargoDryInventoryMovementsBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetInventoryMovementsAsync(
            request.ProviderProfileId,
            request.ProductCode,
            request.BatchCode,
            request.MovementType,
            request.DateFrom,
            request.DateTo,
            request.Page,
            request.PageSize,
            ct);

        return new GetCargoDryInventoryMovementsBffResponse { PagedResult = result };
    }
}
