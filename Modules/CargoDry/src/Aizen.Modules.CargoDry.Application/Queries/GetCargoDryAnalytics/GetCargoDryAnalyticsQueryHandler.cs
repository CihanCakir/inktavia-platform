using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryAnalytics;

public sealed class GetCargoDryAnalyticsQueryHandler
    : AizenQueryHandler<GetCargoDryAnalyticsQuery, GetCargoDryAnalyticsResponse>
{
    private const string CacheKey = "cargodry:analytics:snapshot";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryProductRepository _products;
    private readonly IAizenDistributedCache     _cache;

    public GetCargoDryAnalyticsQueryHandler(
        ICargoDryKitRepository kits,
        ICargoDryProductRepository products,
        IAizenDistributedCache cache)
    {
        _kits     = kits;
        _products = products;
        _cache    = cache;
    }

    public override async Task<GetCargoDryAnalyticsResponse> Handle(
        GetCargoDryAnalyticsQuery request, CancellationToken ct)
    {
        var (hit, cached) = await _cache.TryGetAsync<GetCargoDryAnalyticsResponse>(CacheKey, ct);
        if (hit) return cached;

        var allKits  = await _kits.GetAllAsync(ct);
        var allProd  = await _products.GetAllActiveAsync(ct);
        var prodMap  = allProd.ToDictionary(p => p.ProductCode);
        var now      = DateTimeOffset.UtcNow;

        var statusDist = allKits
            .GroupBy(k => k.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var thirtyAgo = now.AddDays(-30);
        var daily     = allKits
            .Where(k => k.ActivatedAt >= thirtyAgo)
            .GroupBy(k => DateOnly.FromDateTime(k.ActivatedAt!.Value.UtcDateTime))
            .Select(g => new CargoDryDailyActivationPoint
            {
                Date        = g.Key,
                Activations = g.Count(),
                Renewals    = g.Count(k => k.RenewalCount > 0),
            })
            .OrderBy(p => p.Date)
            .ToList();

        var activeKits = allKits.Where(k => k.Status == CargoDryKitStatus.Activated).ToList();
        var buckets    = new Dictionary<string, int>
        {
            ["0–25%"]   = 0,
            ["25–50%"]  = 0,
            ["50–75%"]  = 0,
            ["75–100%"] = 0,
        };
        foreach (var k in activeKits)
        {
            var pct = k.EfficiencyPercent;
            var key = pct < 25 ? "0–25%" : pct < 50 ? "25–50%" : pct < 75 ? "50–75%" : "75–100%";
            buckets[key]++;
        }

        var productMix = allKits
            .GroupBy(k => k.ProductCode)
            .Select(g => new CargoDryProductMixRow
            {
                ProductCode = g.Key,
                ProductName = prodMap.TryGetValue(g.Key, out var p) ? p.Name : g.Key,
                ActiveKits  = g.Count(k => k.Status == CargoDryKitStatus.Activated),
                TotalKits   = g.Count(),
            })
            .OrderByDescending(r => r.TotalKits)
            .ToList();

        double avgEff      = activeKits.Count > 0 ? activeKits.Average(k => k.EfficiencyPercent) : 0d;
        var activatedTotal = allKits.Count(k => k.Status != CargoDryKitStatus.Available);
        var renewed        = allKits.Count(k => k.RenewalCount > 0);
        var expiring30     = allKits.Count(k =>
            k.Status == CargoDryKitStatus.Activated &&
            k.ExpiresAt.HasValue &&
            k.ExpiresAt.Value <= now.AddDays(30));

        var analytics = new CargoDryAnalyticsDto
        {
            StatusDistribution  = statusDist,
            DailyActivations    = daily,
            EfficiencyBuckets   = buckets,
            ProductMix          = productMix,
            TotalKits           = allKits.Count,
            ActiveKits          = activeKits.Count,
            AvgEfficiencyPct    = Math.Round(avgEff, 1),
            RenewalRatePct      = activatedTotal > 0
                ? Math.Round(renewed / (double)activatedTotal * 100d, 1)
                : 0d,
            ExpiringNext30Days  = expiring30,
            ComputedAt          = now,
        };

        var response = new GetCargoDryAnalyticsResponse { Analytics = analytics };

        await _cache.SetAsync(response, CacheKey,
            new AizenCacheOptions { AbsoluteExpirationRelativeToNow = CacheTtl }, ct);

        return response;
    }
}
