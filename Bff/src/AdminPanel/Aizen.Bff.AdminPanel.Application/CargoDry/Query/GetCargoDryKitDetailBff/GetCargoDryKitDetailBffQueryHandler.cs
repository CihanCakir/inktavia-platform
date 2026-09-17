using Aizen.Bff.AdminPanel.Application.CargoDry.Services;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryKitDetailBff;

[DocumentationInfo("Get CargoDry kit detail BFF query handler",
    "Proxies GET /api/v1/cargodry/admin/kits/{id} to the CargoDry module and returns the full " +
    "kit detail record including admin-only fields (QR payload, revocation details, timestamps), " +
    "then enriches VesselName (GEMİ) and owner display name (SAHİP) from the Vessel/Identity modules " +
    "(the CargoDry module returns VesselId/OwnerUserId but leaves the names null). Phase 8B (July 2026).")]
public sealed class GetCargoDryKitDetailBffQueryHandler
    : AizenQueryHandler<GetCargoDryKitDetailBffQuery, GetCargoDryKitDetailBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;
    private readonly IVesselRemoteCall _vessel;
    private readonly IIdentityRemoteCall _identity;

    public GetCargoDryKitDetailBffQueryHandler(
        ICargoDryRemoteCall remote, IVesselRemoteCall vessel, IIdentityRemoteCall identity)
    {
        _remote = remote;
        _vessel = vessel;
        _identity = identity;
    }

    public override async Task<GetCargoDryKitDetailBffResponse> Handle(
        GetCargoDryKitDetailBffQuery request, CancellationToken ct)
    {
        var result = await _remote.GetKitDetailAsync(request.KitId, ct);
        var kit = result?.Kit;

        // Fill GEMİ/SAHİP: resolve the kit's vessel + owner display names (the module leaves them null).
        if (kit is not null)
        {
            var vesselIds = kit.VesselId is > 0 ? new[] { kit.VesselId.Value } : Array.Empty<long>();
            var ownerIds  = kit.OwnerUserId is > 0 ? new[] { kit.OwnerUserId.Value } : Array.Empty<long>();

            var (vesselNames, ownerNames) = await CargoDryKitAdminNameResolver.ResolveAsync(
                _vessel, _identity, vesselIds, ownerIds);

            if (string.IsNullOrEmpty(kit.VesselName) && kit.VesselId is > 0
                && vesselNames.TryGetValue(kit.VesselId.Value, out var vn))
                kit.VesselName = vn;

            if (string.IsNullOrEmpty(kit.OwnerDisplayName) && kit.OwnerUserId is > 0
                && ownerNames.TryGetValue(kit.OwnerUserId.Value, out var on))
                kit.OwnerDisplayName = on;
        }

        return new GetCargoDryKitDetailBffResponse { Kit = kit };
    }
}
