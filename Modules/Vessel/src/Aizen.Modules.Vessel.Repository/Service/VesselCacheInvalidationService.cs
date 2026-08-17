using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Security;
using Aizen.Modules.Vessel.Domain.Interface.Service;

namespace Aizen.Modules.Vessel.Repository.Service;

/// <summary>
/// Invalidates caches stored by the CQRS query-handler read decorator.
///
/// Keys are built via <see cref="AizenQueryCacheKey"/> — the SAME canonical generator the
/// read path uses — so eviction targets the exact entry the read stored. The pairs passed to
/// <see cref="AizenQueryCacheKey.For"/> MUST list every non-<c>QueryId</c> query property in
/// declaration order, using the same default values the read path supplies; omitting a
/// property (previously the paged props) produces a different hash and the eviction misses.
///
/// Paged reads are cached per (pageIndex, pageSize, …) tuple. A single write can only evict a
/// specific tuple, so these methods evict the default first-page variant that the mobile/list
/// surfaces request (pageIndex 0, pageSize 20; access-url flags off/15m). Non-default page
/// variants fall back to the query's TTL — the read path has no wildcard eviction.
/// </summary>
public sealed class VesselCacheInvalidationService : IVesselCacheInvalidationService
{
    // Read-path defaults for paged queries (see the Vessel query constructors / controllers).
    private const int DefaultPageIndex = 0;
    private const int DefaultPageSize = 20;
    private const bool DefaultIncludeAccessUrls = false;
    private const int DefaultAccessUrlExpiresInMinutes = 15;

    private readonly IAizenDistributedCache _cache;

    public VesselCacheInvalidationService(IAizenDistributedCache cache) => _cache = cache;

    public async Task InvalidateVesselAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveReadCacheEntry(AizenQueryCacheKey.For("GetVesselDetailQueryHandler", ("VesselId", vesselId)));
        await _cache.RemoveReadCacheEntry(AizenQueryCacheKey.For("GetVesselByIdQueryHandler", ("VesselId", vesselId)));
    }

    public async Task InvalidateVesselByCodeAsync(string vesselCode, CancellationToken ct = default)
    {
        await _cache.RemoveReadCacheEntry(AizenQueryCacheKey.For("GetVesselByCodeQueryHandler", ("VesselCode", vesselCode)));
    }

    public async Task InvalidateUserVesselListAsync(long userId, CancellationToken ct = default)
    {
        await _cache.RemoveReadCacheEntry(AizenQueryCacheKey.For(
            "GetUserVesselsQueryHandler",
            ("UserId", userId),
            ("PageIndex", DefaultPageIndex),
            ("PageSize", DefaultPageSize)));
    }

    public async Task InvalidateOwnersAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveReadCacheEntry(AizenQueryCacheKey.For(
            "GetVesselOwnersQueryHandler",
            ("VesselId", vesselId),
            ("PageIndex", DefaultPageIndex),
            ("PageSize", DefaultPageSize)));
    }

    public async Task InvalidateSpecificationAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveReadCacheEntry(AizenQueryCacheKey.For("GetVesselSpecificationQueryHandler", ("VesselId", vesselId)));
    }

    public async Task InvalidateEnginesAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveReadCacheEntry(AizenQueryCacheKey.For(
            "GetVesselEnginesQueryHandler",
            ("VesselId", vesselId),
            ("PageIndex", DefaultPageIndex),
            ("PageSize", DefaultPageSize)));
    }

    public async Task InvalidateDocumentsAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveReadCacheEntry(AizenQueryCacheKey.For(
            "GetVesselDocumentsQueryHandler",
            ("VesselId", vesselId),
            ("PageIndex", DefaultPageIndex),
            ("PageSize", DefaultPageSize),
            ("IncludeAccessUrls", DefaultIncludeAccessUrls),
            ("AccessUrlExpiresInMinutes", DefaultAccessUrlExpiresInMinutes)));
    }

    public async Task InvalidateMediaAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveReadCacheEntry(AizenQueryCacheKey.For(
            "GetVesselMediaQueryHandler",
            ("VesselId", vesselId),
            ("PageIndex", DefaultPageIndex),
            ("PageSize", DefaultPageSize),
            ("IncludeAccessUrls", DefaultIncludeAccessUrls),
            ("AccessUrlExpiresInMinutes", DefaultAccessUrlExpiresInMinutes)));
    }

    public async Task InvalidateLocationAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveReadCacheEntry(AizenQueryCacheKey.For("GetCurrentVesselLocationQueryHandler", ("VesselId", vesselId)));
    }

    public async Task InvalidateStatusHistoryAsync(long vesselId, CancellationToken ct = default)
    {
        await _cache.RemoveReadCacheEntry(AizenQueryCacheKey.For(
            "GetVesselStatusHistoryQueryHandler",
            ("VesselId", vesselId),
            ("PageIndex", DefaultPageIndex),
            ("PageSize", DefaultPageSize)));
    }
}
