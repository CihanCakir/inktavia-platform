using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.LookupCargoDryKitAdminBff;

[DocumentationInfo("Lookup CargoDry kit admin BFF query handler",
    "Proxies GET /api/v1/cargodry/admin/kits/lookup?q={query} to the CargoDry module. " +
    "Supports lookup by numeric id, exact kit code, or exact serial number. " +
    "Always returns a result object — check Found before reading Kit. " +
    "Phase 8B (July 2026).")]
public sealed class LookupCargoDryKitAdminBffQueryHandler
    : AizenQueryHandler<LookupCargoDryKitAdminBffQuery, LookupCargoDryKitAdminBffResponse>
{
    private readonly IAdminCargoDryBffRemoteCall _remote;

    public LookupCargoDryKitAdminBffQueryHandler(IAdminCargoDryBffRemoteCall remote)
        => _remote = remote;

    public override async Task<LookupCargoDryKitAdminBffResponse> Handle(
        LookupCargoDryKitAdminBffQuery request, CancellationToken ct)
    {
        var result = await _remote.LookupKitAsync(request.Query, ct);
        return new LookupCargoDryKitAdminBffResponse { Result = result?.Result };
    }
}
