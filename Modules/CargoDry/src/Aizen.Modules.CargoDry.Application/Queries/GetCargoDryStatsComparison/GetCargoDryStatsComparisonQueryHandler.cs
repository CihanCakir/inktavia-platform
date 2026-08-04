using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStatsComparison;

/// <summary>
/// Compares current 30-day window vs prior 30-day window to produce
/// percentage change values for dashboard KPI badges.
/// Computed from in-memory kit list (MVP). Cache TTL: 15 minutes.
/// </summary>
public sealed class GetCargoDryStatsComparisonQueryHandler
    : AizenQueryHandler<GetCargoDryStatsComparisonQuery, CargoDryStatsComparisonDto>
{
    private const string CacheKey = "cargodry:stats:comparison";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    private readonly ICargoDryKitRepository _kits;
    private readonly IAizenDistributedCache _cache;

    public GetCargoDryStatsComparisonQueryHandler(
        ICargoDryKitRepository kits,
        IAizenDistributedCache cache)
    {
        _kits  = kits;
        _cache = cache;
    }

    public override async Task<CargoDryStatsComparisonDto> Handle(
        GetCargoDryStatsComparisonQuery request, CancellationToken ct)
    {
        var (hit, cached) = await _cache.TryGetAsync<CargoDryStatsComparisonDto>(CacheKey, ct);
        if (hit) return cached;

        var now        = DateTimeOffset.UtcNow;
        var curStart   = now.AddDays(-30);
        var priorStart = now.AddDays(-60);

        var all = await _kits.GetAllAsync(ct);

        // Kits activated within each 30-day window (regardless of current status)
        var curActivated   = all.Count(k => k.ActivatedAt.HasValue && k.ActivatedAt >= curStart);
        var priorActivated = all.Count(k => k.ActivatedAt.HasValue
                                         && k.ActivatedAt >= priorStart
                                         && k.ActivatedAt < curStart);

        // Active kits (status = Activated) in each window
        var curActive  = all.Count(k => k.Status == CargoDryKitStatus.Activated
                                     && k.ActivatedAt.HasValue && k.ActivatedAt >= curStart);
        var priorActive = all.Count(k => k.Status == CargoDryKitStatus.Activated
                                      && k.ActivatedAt.HasValue
                                      && k.ActivatedAt >= priorStart
                                      && k.ActivatedAt < curStart);

        // Total kits manufactured in each window
        var curTotal  = all.Count(k => k.ManufacturedAt >= curStart);
        var priorTotal = all.Count(k => k.ManufacturedAt >= priorStart
                                     && k.ManufacturedAt < curStart);

        // Today's activations vs same calendar day 30d ago
        // Build UTC midnight explicitly from UTC components (now is DateTimeOffset.UtcNow) — avoids .Date's
        // Kind=Unspecified footgun and keeps the boundary correct regardless of server locale.
        var todayStart     = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var todayEnd       = todayStart.AddDays(1);
        var priorDayStart  = todayStart.AddDays(-30);
        var priorDayEnd    = todayStart.AddDays(-29);

        var todayCur   = all.Count(k => k.ActivatedAt.HasValue
                                     && k.ActivatedAt >= todayStart
                                     && k.ActivatedAt < todayEnd);
        var todayPrior = all.Count(k => k.ActivatedAt.HasValue
                                     && k.ActivatedAt >= priorDayStart
                                     && k.ActivatedAt < priorDayEnd);

        // Renewal rate: renewals / activations in each window
        var curRenewals  = all.Count(k => k.ActivatedAt.HasValue && k.ActivatedAt >= curStart   && k.RenewalCount > 0);
        var priorRenewals = all.Count(k => k.ActivatedAt.HasValue
                                        && k.ActivatedAt >= priorStart && k.ActivatedAt < curStart
                                        && k.RenewalCount > 0);

        var curRenewalRate   = curActivated   > 0 ? (double)curRenewals   / curActivated   * 100 : 0d;
        var priorRenewalRate = priorActivated > 0 ? (double)priorRenewals / priorActivated * 100 : 0d;

        var dto = new CargoDryStatsComparisonDto
        {
            ActiveKitsChangePercent   = ComputeChange(priorActive, curActive),
            TotalKitsChangePercent    = ComputeChange(priorTotal,  curTotal),
            TodayActivationsChangePct = ComputeChange(todayPrior,  todayCur),
            RenewalRateDelta          = Math.Round(curRenewalRate - priorRenewalRate, 1),
            CurrentPeriodStart        = curStart.ToString("O"),
            PriorPeriodStart          = priorStart.ToString("O"),
        };

        await _cache.SetAsync(dto, CacheKey,
            new AizenCacheOptions { AbsoluteExpirationRelativeToNow = CacheTtl }, ct);

        return dto;
    }

    private static double? ComputeChange(int prior, int current)
    {
        if (prior == 0) return current > 0 ? 100d : null;
        return Math.Round((current - prior) / (double)prior * 100, 1);
    }
}
