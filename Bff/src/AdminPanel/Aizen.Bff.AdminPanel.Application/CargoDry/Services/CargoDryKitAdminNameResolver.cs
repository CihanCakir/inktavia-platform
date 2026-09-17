using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Services;

/// <summary>
/// Enriches admin CargoDry kit rows with the cross-module names the module leaves null: VesselId → vessel name and
/// OwnerUserId → owner display name. Batched (one Vessel call + one Identity call) so the kit LIST resolves N vessels /
/// owners in two round-trips, not 2N. Best-effort: a failed lookup leaves the name null (the FE falls back to the id),
/// never throws — the admin kit read must not 500 because a name service is down.
/// </summary>
public static class CargoDryKitAdminNameResolver
{
    /// <summary>
    /// Resolves the given vessel ids and owner user ids to display names in two batched calls. Returns two maps
    /// (vesselId → name, ownerUserId → "First Last"); a missing/failed lookup simply omits the entry.
    /// </summary>
    public static async Task<(Dictionary<long, string> VesselNames, Dictionary<long, string> OwnerNames)> ResolveAsync(
        IVesselRemoteCall vessel,
        IIdentityRemoteCall identity,
        IReadOnlyCollection<long> vesselIds,
        IReadOnlyCollection<long> ownerUserIds)
    {
        var vesselTask = vesselIds.Count > 0
            ? FetchVesselNamesAsync(vessel, vesselIds)
            : Task.FromResult(new Dictionary<long, string>());
        var ownerTask = ownerUserIds.Count > 0
            ? FetchOwnerNamesAsync(identity, ownerUserIds)
            : Task.FromResult(new Dictionary<long, string>());

        await Task.WhenAll(vesselTask, ownerTask);
        return (vesselTask.Result, ownerTask.Result);
    }

    private static async Task<Dictionary<long, string>> FetchVesselNamesAsync(
        IVesselRemoteCall vessel, IReadOnlyCollection<long> vesselIds)
    {
        try
        {
            var result = await vessel.GetVesselNamesByIds(vesselIds.ToArray());
            return (result.Body ?? [])
                .GroupBy(v => v.VesselId)
                .ToDictionary(g => g.Key, g => g.First().Name);
        }
        catch { return new Dictionary<long, string>(); }
    }

    private static async Task<Dictionary<long, string>> FetchOwnerNamesAsync(
        IIdentityRemoteCall identity, IReadOnlyCollection<long> ownerUserIds)
    {
        try
        {
            var result = await identity.GetUserProfilesByUserIds(ownerUserIds.ToArray());
            return (result.Body ?? [])
                .GroupBy(p => p.UserId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var p = g.First();
                        return $"{p.FirstName} {p.LastName}".Trim();
                    });
        }
        catch { return new Dictionary<long, string>(); }
    }
}
