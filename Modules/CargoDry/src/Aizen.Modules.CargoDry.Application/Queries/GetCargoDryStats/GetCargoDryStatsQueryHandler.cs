using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStats;

public sealed class GetCargoDryStatsQueryHandler
    : AizenQueryHandler<GetCargoDryStatsQuery, CargoDryStatsDto>
{
    private const string CacheKey = "cargodry:stats:global";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    private readonly ICargoDryKitRepository   _kits;
    private readonly ICargoDryBatchRepository _batches;
    private readonly IAizenDistributedCache   _cache;

    public GetCargoDryStatsQueryHandler(
        ICargoDryKitRepository kits,
        ICargoDryBatchRepository batches,
        IAizenDistributedCache cache)
    {
        _kits    = kits;
        _batches = batches;
        _cache   = cache;
    }

    public override async Task<CargoDryStatsDto> Handle(
        GetCargoDryStatsQuery request, CancellationToken ct)
    {
        var (hit, cached) = await _cache.TryGetAsync<CargoDryStatsDto>(CacheKey, ct);
        if (hit) return cached;

        var stats         = await _kits.GetStatsAsync(ct: ct);
        var activeBatches = await _batches.CountActiveBatchesAsync(ct);

        var result = new CargoDryStatsDto
        {
            TotalKits          = stats.Total,
            AvailableKits      = stats.Available,
            ActiveKits         = stats.Active,
            ExpiringKits       = stats.Expiring,
            ExpiredKits        = stats.Expired,
            RevokedKits        = stats.Revoked,
            TodayActivations   = stats.TodayActivations,
            TotalBatches       = activeBatches,
            RenewalRatePercent = stats.Total > 0
                ? Math.Round(stats.WithRenewals / (double)stats.Total * 100, 1)
                : 0,
        };

        await _cache.SetAsync(result, CacheKey,
            new AizenCacheOptions { AbsoluteExpirationRelativeToNow = CacheTtl }, ct);

        return result;
    }
}
