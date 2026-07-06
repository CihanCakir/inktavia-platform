using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryOperationalOverview;

/// <summary>
/// Computes the rich operational KPI overview at query time.
/// Cached for 2 minutes — same TTL as the stats query.
/// Phase 9A — dashboard KPI refresh.
/// </summary>
public sealed class GetCargoDryOperationalOverviewQueryHandler
    : AizenQueryHandler<GetCargoDryOperationalOverviewQuery, CargoDryOperationalOverviewDto>
{
    private const string CacheKey = "cargodry:operational:overview";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    private readonly ICargoDryKitRepository               _kits;
    private readonly ICargoDryBatchRepository             _batches;
    private readonly ICargoDryKitLifecycleEventRepository _lifecycleEvents;
    private readonly IAizenDistributedCache               _cache;

    public GetCargoDryOperationalOverviewQueryHandler(
        ICargoDryKitRepository               kits,
        ICargoDryBatchRepository             batches,
        ICargoDryKitLifecycleEventRepository lifecycleEvents,
        IAizenDistributedCache               cache)
    {
        _kits            = kits;
        _batches         = batches;
        _lifecycleEvents = lifecycleEvents;
        _cache           = cache;
    }

    public override async Task<CargoDryOperationalOverviewDto> Handle(
        GetCargoDryOperationalOverviewQuery request, CancellationToken ct)
    {
        var (hit, cached) = await _cache.TryGetAsync<CargoDryOperationalOverviewDto>(CacheKey, ct);
        if (hit) return cached;

        // Sequential — DbContext is not thread-safe; concurrent awaits on the same instance crash.
        var stats                      = await _kits.GetStatsAsync(ct);
        var expiringSoon               = await _kits.GetExpiringAsync(30, ct);
        var (_, commercialReviewCount) = await _kits.GetPagedAsync(
            CargoDryKitStatus.CommercialReviewRequired,
            null, null, null, null, 0, 0, ct);
        var (_, lostCount)             = await _kits.GetPagedAsync(
            CargoDryKitStatus.Lost,
            null, null, null, null, 0, 0, ct);
        var activeBatches              = await _batches.CountActiveBatchesAsync(ct);
        var recentLifecycleEventCount  = await _lifecycleEvents.CountRecentAsync(24, ct);

        // Alert count = critical expiring (≤7d) + commercial review + expired unmarked
        var criticalExpiring   = expiringSoon.Count(k =>
            k.ExpiresAt.HasValue &&
            (k.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays <= 7);
        var openAlertCount = criticalExpiring + commercialReviewCount + stats.Expired;

        var result = new CargoDryOperationalOverviewDto
        {
            // Kit lifecycle counts
            TotalKits                    = stats.Total,
            AvailableKits                = stats.Available,
            ActivatedKits                = stats.Active,
            ExpiredKits                  = stats.Expired,
            RevokedKits                  = stats.Revoked,
            LostKits                     = lostCount,
            RenewalDueSoonKits           = expiringSoon.Count,
            CommercialReviewRequiredKits = commercialReviewCount,
            ProviderHeldKits             = 0,  // StockLocationType filter not yet in repo; post-MVP
            WarehouseStockKits           = 0,  // StockLocationType filter not yet in repo; post-MVP

            // Batch counts
            TotalBatches             = activeBatches,  // non-revoked batches
            ActiveBatches            = activeBatches,
            ProviderAllocatedBatches = 0,              // post-MVP (needs batch allocation tracking)

            // Activity indicators
            RecentLifecycleEventCount = recentLifecycleEventCount,
            OpenOperationalAlertCount = openAlertCount,

            ComputedAtUtc = DateTimeOffset.UtcNow,
        };

        await _cache.SetAsync(result, CacheKey,
            new AizenCacheOptions { AbsoluteExpirationRelativeToNow = CacheTtl }, ct);

        return result;
    }
}
