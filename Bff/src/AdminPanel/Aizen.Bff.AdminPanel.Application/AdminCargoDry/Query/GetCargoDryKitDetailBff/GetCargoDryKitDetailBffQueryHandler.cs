using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryKitDetailBff;

[DocumentationInfo("Get CargoDry kit detail BFF query handler",
    "Proxies GET /api/v1/cargodry/admin/kits/{id} to the CargoDry module and returns the full " +
    "kit detail record including admin-only fields (QR payload, revocation details, timestamps). " +
    "Phase 8B (July 2026).")]
public sealed class GetCargoDryKitDetailBffQueryHandler
    : AizenQueryHandler<GetCargoDryKitDetailBffQuery, GetCargoDryKitDetailBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryKitDetailBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryKitDetailBffResponse> Handle(
        GetCargoDryKitDetailBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetKitDetailAsync(request.KitId, ct);
        return new GetCargoDryKitDetailBffResponse { Kit = result?.Kit };
    }
}
