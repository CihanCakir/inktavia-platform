using System.Security.Cryptography;
using System.Text;
using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.Vessel.Abstraction.Model;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Repository.Service;

[DocumentationInfo("Vessel cache invalidation service", "Invalidates cached query results when vessel data changes.")]
public sealed class VesselCacheInvalidationService : IVesselCacheInvalidationService
{
    private readonly IAizenDistributedCache _cache;

    public VesselCacheInvalidationService(IAizenDistributedCache cache) => _cache = cache;

    private static string HandlerKey(string handlerTypeName, string propString)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(propString));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.AppendFormat("{0:x2}", b);
        return $"{handlerTypeName}:{sb}";
    }

    private static string HandlerKeyNoProps(string handlerTypeName) => $"{handlerTypeName}:";

    public async Task InvalidateVesselAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetVesselDetailQueryHandler", $"VesselId_{vesselId}|"));
        await _cache.RemoveNoHash(HandlerKey("GetVesselByIdQueryHandler", $"VesselId_{vesselId}|"));
    }

    public async Task InvalidateVesselByCodeAsync(string vesselCode, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetVesselByCodeQueryHandler", $"VesselCode_{vesselCode}|"));
    }

    public async Task InvalidateUserVesselListAsync(long userId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetUserVesselsQueryHandler", $"UserId_{userId}|"));
    }

    public async Task InvalidateOwnersAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetVesselOwnersQueryHandler", $"VesselId_{vesselId}|"));
    }

    public async Task InvalidateSpecificationAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetVesselSpecificationQueryHandler", $"VesselId_{vesselId}|"));
    }

    public async Task InvalidateEnginesAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetVesselEnginesQueryHandler", $"VesselId_{vesselId}|"));
    }

    public async Task InvalidateDocumentsAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetVesselDocumentsQueryHandler", $"VesselId_{vesselId}|"));
    }

    public async Task InvalidateMediaAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetVesselMediaQueryHandler", $"VesselId_{vesselId}|"));
    }

    public async Task InvalidateLocationAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetCurrentVesselLocationQueryHandler", $"VesselId_{vesselId}|"));
    }

    public async Task InvalidateStatusHistoryAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(HandlerKey("GetVesselStatusHistoryQueryHandler", $"VesselId_{vesselId}|"));
    }
}
