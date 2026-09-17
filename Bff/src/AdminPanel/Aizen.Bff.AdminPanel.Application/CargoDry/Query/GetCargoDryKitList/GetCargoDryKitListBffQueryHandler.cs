using Aizen.Bff.AdminPanel.Application.CargoDry.Services;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryKitList;

[DocumentationInfo("Get CargoDry kit list BFF query handler",
    "Calls the CargoDry admin kit list endpoint with filter/pagination parameters, then enriches each row's " +
    "VesselName (GEMİ) and owner display name (SAHİP) from the Vessel/Identity modules — the CargoDry module returns " +
    "VesselId/OwnerUserId but leaves the names null.")]
public sealed class GetCargoDryKitListBffQueryHandler
    : AizenQueryHandler<GetCargoDryKitListBffQuery, GetCargoDryKitListBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;
    private readonly IVesselRemoteCall _vessel;
    private readonly IIdentityRemoteCall _identity;

    public GetCargoDryKitListBffQueryHandler(
        ICargoDryRemoteCall remote, IVesselRemoteCall vessel, IIdentityRemoteCall identity)
    {
        _remote = remote;
        _vessel = vessel;
        _identity = identity;
    }

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

        // Enrich the GEMİ/SAHİP columns: resolve the distinct vessel + owner ids on this page in two batched calls.
        var items = result?.Items;
        if (items is { Count: > 0 })
        {
            var vesselIds = items.Where(k => k.VesselId is > 0).Select(k => k.VesselId!.Value).Distinct().ToArray();
            var ownerIds  = items.Where(k => k.OwnerUserId is > 0).Select(k => k.OwnerUserId!.Value).Distinct().ToArray();

            var (vesselNames, ownerNames) = await CargoDryKitAdminNameResolver.ResolveAsync(
                _vessel, _identity, vesselIds, ownerIds);

            foreach (var kit in items)
            {
                if (string.IsNullOrEmpty(kit.VesselName) && kit.VesselId is > 0
                    && vesselNames.TryGetValue(kit.VesselId.Value, out var vn))
                    kit.VesselName = vn;

                if (string.IsNullOrEmpty(kit.OwnerDisplayName) && kit.OwnerUserId is > 0
                    && ownerNames.TryGetValue(kit.OwnerUserId.Value, out var on))
                    kit.OwnerDisplayName = on;
            }
        }

        return new GetCargoDryKitListBffResponse { KitList = result };
    }
}
