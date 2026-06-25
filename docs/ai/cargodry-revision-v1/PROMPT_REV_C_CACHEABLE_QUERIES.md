# PROMPT REV-C — CargoDry Cacheable Queries
# AizenQueryHandlerCacheable + IAizenCache Fallback Pattern

## Context & Discovery Step

Before implementing, perform this discovery in the CargoDry solution:

```bash
grep -r "AizenQueryHandlerCacheable" \
  Modules/CargoDry/src/ \
  Core/ \
  --include="*.cs" | head -20
```

**Case A — AizenQueryHandlerCacheable exists:**
Use it as the base class for cacheable handlers (see Section A below).

**Case B — AizenQueryHandlerCacheable does not exist:**
Implement caching directly via `IAizenCache` injection inside the handler (see Section B below).

Both sections produce functionally equivalent results. Apply whichever matches the discovered state.

---

## Cacheable Query Candidates

| Query | Cache Key | TTL | Invalidated By |
|-------|-----------|-----|----------------|
| `GetCargoDryStatsQuery` | `cargodry:stats:global` | 2 min | `ActivateKit`, `RevokeKit`, `KitExpiredMarkingJob` |
| `GetCargoDryProductListQuery` *(new)* | `cargodry:products:all` | 30 min | `UpdateProduct`, `DeactivateProduct` |
| `GetCargoDryAnalyticsQuery` *(from extensions)* | `cargodry:analytics:snapshot` | 10 min | `DailySnapshotJob` completion |
| `GetAdminKitListQuery` | ❌ NOT cacheable | — | User-specific, filtered, paginated |
| `GetMyKitsQuery` | ❌ NOT cacheable | — | Per-user live state |

---

## SECTION A — Using AizenQueryHandlerCacheable Base Class

*Apply this if `AizenQueryHandlerCacheable<TQuery, TResponse>` is found in the codebase.*

### A.1 GetCargoDryStatsQuery — Cacheable

**File:** `Application/Queries/GetCargoDryStats/GetCargoDryStatsQueryHandler.cs`

```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStats;

[DocumentationInfo("Get CargoDry Stats Query Handler",
    "Returns global kit status counters. Result is cached for 2 minutes to reduce DB pressure " +
    "on high-frequency dashboard polling.")]
public sealed class GetCargoDryStatsQueryHandler
    : AizenQueryHandlerCacheable<GetCargoDryStatsQuery, CargoDryStatsDto>
{
    private readonly ICargoDryKitRepository   _kits;
    private readonly ICargoDryBatchRepository _batches;

    public GetCargoDryStatsQueryHandler(
        ICargoDryKitRepository kits, ICargoDryBatchRepository batches)
    {
        _kits    = kits;
        _batches = batches;
    }

    public override string CacheKey(GetCargoDryStatsQuery request) => "cargodry:stats:global";
    public override TimeSpan CacheExpiry => TimeSpan.FromMinutes(2);

    public override async Task<CargoDryStatsDto> HandleCore(
        GetCargoDryStatsQuery request, CancellationToken ct)
    {
        var stats          = await _kits.GetStatsAsync(ct);
        var batches        = await _batches.GetAllAsync(ct);
        var activeBatches  = batches.Count(b => !b.IsRevoked);

        var renewalsCount  = 0; // TODO: from ICargoDryRenewalRepository after that repo is added
        var renewalRate    = stats.Active > 0
            ? Math.Round((double)renewalsCount / stats.Active * 100, 1)
            : 0;

        return new CargoDryStatsDto
        {
            TotalKits          = stats.Total,
            AvailableKits      = stats.Available,
            ActiveKits         = stats.Active,
            ExpiringKits       = stats.Expiring,
            ExpiredKits        = stats.Expired,
            RevokedKits        = stats.Revoked,
            TodayActivations   = stats.TodayActivations,
            TotalBatches       = activeBatches,
            RenewalRatePercent = renewalRate,
        };
    }
}
```

### A.2 GetCargoDryProductListQuery — New, Cacheable

**File:** `Application/Queries/GetCargoDryProductList/GetCargoDryProductListQuery.cs`

```csharp
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProductList;

/// <summary>Returns the active product catalog. Used by batch generation UI and mobile onboarding.</summary>
public sealed class GetCargoDryProductListQuery : AizenQuery<List<CargoDryProductDto>> { }
```

**File:** `Application/Queries/GetCargoDryProductList/GetCargoDryProductListQueryHandler.cs`

```csharp
[DocumentationInfo("Get CargoDry Product List Query Handler",
    "Returns all active products. Cached for 30 minutes — product catalog rarely changes.")]
public sealed class GetCargoDryProductListQueryHandler
    : AizenQueryHandlerCacheable<GetCargoDryProductListQuery, List<CargoDryProductDto>>
{
    private readonly ICargoDryProductRepository _products;

    public GetCargoDryProductListQueryHandler(ICargoDryProductRepository products)
        => _products = products;

    public override string CacheKey(GetCargoDryProductListQuery request) => "cargodry:products:all";
    public override TimeSpan CacheExpiry => TimeSpan.FromMinutes(30);

    public override async Task<List<CargoDryProductDto>> HandleCore(
        GetCargoDryProductListQuery request, CancellationToken ct)
    {
        var products = await _products.GetAllActiveAsync(ct);
        return products.Select(p => new CargoDryProductDto
        {
            Id           = p.Id,
            ProductCode  = p.ProductCode,
            Name         = p.Name,
            Description  = p.Description,
            ValidityDays = p.ValidityDays,
            HasSmartDevice = p.HasSmartDevice,
            RetailPrice  = p.RetailPrice,
            CurrencyCode = p.CurrencyCode,
            IsActive     = p.IsActive,
        }).ToList();
    }
}
```

### A.3 GetCargoDryAnalyticsQuery — Cacheable

This query is defined in `cargodry-extensions-v1/PROMPT_2_CARGODRY_ANALYTICS.md`.
Update its handler to use the cacheable base class.

**File:** `Application/Queries/GetCargoDryAnalytics/GetCargoDryAnalyticsQueryHandler.cs`

```csharp
[DocumentationInfo("Get CargoDry Analytics Query Handler",
    "Returns the pre-computed analytics snapshot for the admin dashboard charts. " +
    "Reads from MongoDB snapshot collection. Cached for 10 minutes.")]
public sealed class GetCargoDryAnalyticsQueryHandler
    : AizenQueryHandlerCacheable<GetCargoDryAnalyticsQuery, CargoDryAnalyticsDto>
{
    private readonly ICargoDrySnapshotRepository _snapshots;
    private readonly ICargoDryKitRepository      _kits;

    public GetCargoDryAnalyticsQueryHandler(
        ICargoDrySnapshotRepository snapshots,
        ICargoDryKitRepository kits)
    {
        _snapshots = snapshots;
        _kits      = kits;
    }

    public override string CacheKey(GetCargoDryAnalyticsQuery request) => "cargodry:analytics:snapshot";
    public override TimeSpan CacheExpiry => TimeSpan.FromMinutes(10);

    public override async Task<CargoDryAnalyticsDto> HandleCore(
        GetCargoDryAnalyticsQuery request, CancellationToken ct)
    {
        // Last 30 days of snapshots from MongoDB
        var snapshots = await _snapshots.GetRecentAsync(30, ct);
        var stats     = await _kits.GetStatsAsync(ct);

        var dailyActivations = snapshots
            .OrderBy(s => s.DateKey)
            .Select(s => new CargoDryDailyActivationPointDto
            {
                Date        = s.DateKey,
                Activations = s.Activations,
                Renewals    = s.Renewals,
            }).ToList();

        // Status distribution from live PG stats
        var statusDistribution = new List<CargoDryStatusSliceDto>
        {
            new() { Name = "Available", Value = stats.Available, Color = "#4ade80" },
            new() { Name = "Active",    Value = stats.Active,    Color = "#60a5fa" },
            new() { Name = "Expiring",  Value = stats.Expiring,  Color = "#fbbf24" },
            new() { Name = "Expired",   Value = stats.Expired,   Color = "#f97316" },
            new() { Name = "Revoked",   Value = stats.Revoked,   Color = "#ef4444" },
        };

        return new CargoDryAnalyticsDto
        {
            StatusDistribution  = statusDistribution,
            DailyActivations    = dailyActivations,
            TotalKits           = stats.Total,
            ActiveKits          = stats.Active,
            ExpiringNext30Days  = stats.Expiring,
            ComputedAt          = DateTimeOffset.UtcNow.ToString("O"),
            // EfficiencyBuckets and ProductMix populated from snapshots if available
            EfficiencyBuckets   = snapshots.LastOrDefault()?.EfficiencyBuckets
                .Select(b => new CargoDryEfficiencyBucketDto { Bucket = b.Bucket, Count = b.Count })
                .ToList() ?? [],
        };
    }
}
```

---

## SECTION B — IAizenCache Direct Injection Fallback

*Apply this if `AizenQueryHandlerCacheable` is NOT found in the codebase.*

Inject `IAizenCache` directly into each handler. The pattern:
1. Try to read from cache — return if hit
2. Execute the actual query
3. Write result to cache with TTL
4. Return result

### B.1 GetCargoDryStatsQuery with IAizenCache

```csharp
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cqrs.Abstractions;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStats;

[DocumentationInfo("Get CargoDry Stats Query Handler",
    "Returns global kit counters. Cached for 2 min via IAizenCache.")]
public sealed class GetCargoDryStatsQueryHandler
    : AizenQueryHandler<GetCargoDryStatsQuery, CargoDryStatsDto>
{
    private const string CacheKey = "cargodry:stats:global";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    private readonly ICargoDryKitRepository   _kits;
    private readonly ICargoDryBatchRepository _batches;
    private readonly IAizenCache              _cache;

    public GetCargoDryStatsQueryHandler(
        ICargoDryKitRepository kits,
        ICargoDryBatchRepository batches,
        IAizenCache cache)
    {
        _kits    = kits;
        _batches = batches;
        _cache   = cache;
    }

    public override async Task<CargoDryStatsDto> Handle(
        GetCargoDryStatsQuery request, CancellationToken ct)
    {
        var cached = await _cache.GetAsync<CargoDryStatsDto>(CacheKey, ct);
        if (cached is not null) return cached;

        var stats         = await _kits.GetStatsAsync(ct);
        var batches       = await _batches.GetAllAsync(ct);
        var activeBatches = batches.Count(b => !b.IsRevoked);

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
            RenewalRatePercent = 0,
        };

        await _cache.SetAsync(CacheKey, result, CacheTtl, ct);
        return result;
    }
}
```

Apply the same pattern for `GetCargoDryProductListQueryHandler` (CacheKey = `"cargodry:products:all"`, TTL = 30 min) and `GetCargoDryAnalyticsQueryHandler` (CacheKey = `"cargodry:analytics:snapshot"`, TTL = 10 min).

---

## STEP 2 — Cache Invalidation on Mutations

When kit state changes (activate, revoke, expire), the stats cache must be invalidated.
Add cache invalidation after `SaveChangesAsync()` in each mutating handler.

### 2.1 ActivateKitCommandHandler — Invalidate Stats

```csharp
// Inject IAizenCache in constructor
private readonly IAizenCache _cache;

// After SaveChangesAsync():
await _cache.RemoveAsync("cargodry:stats:global", ct);
await _cache.RemoveAsync("cargodry:analytics:snapshot", ct);
```

### 2.2 RevokeKitCommandHandler — Invalidate Stats

```csharp
// After SaveChangesAsync():
await _cache.RemoveAsync("cargodry:stats:global", ct);
```

### 2.3 RenewKitCommandHandler — Invalidate Stats

```csharp
// After SaveChangesAsync():
await _cache.RemoveAsync("cargodry:stats:global", ct);
await _cache.RemoveAsync("cargodry:analytics:snapshot", ct);
```

### 2.4 KitExpiredMarkingJob — Invalidate Stats

```csharp
// After kits.SaveChangesAsync():
await _cache.RemoveAsync("cargodry:stats:global", ct);
```

### 2.5 DailySnapshotJob — Invalidate Analytics

```csharp
// After snapshotRepo.UpsertAsync():
await _cache.RemoveAsync("cargodry:analytics:snapshot", ct);
```

---

## STEP 3 — New Controller Endpoint for Product List

**File:** `Controllers/CargoDryAdminController.cs`

Add product list endpoint:

```csharp
/// <summary>
/// Returns the active CargoDry product catalog.
/// GET /api/v1/cargodry/admin/products
/// Cached at handler level — 30 min TTL.
/// </summary>
[HttpGet("products")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetProducts(CancellationToken ct)
{
    var result = await _mediator.Send(new GetCargoDryProductListQuery(), ct);
    return Ok(result);
}
```

---

## Verification Checklist

- [ ] Discovery grep performed — Section A or B applied accordingly
- [ ] `GetCargoDryStatsQueryHandler` returns from cache on cache hit
- [ ] `GetCargoDryStatsQueryHandler` writes to cache with 2 min TTL
- [ ] `GetCargoDryProductListQueryHandler` new query + handler created, cache TTL = 30 min
- [ ] `GetCargoDryAnalyticsQueryHandler` cache TTL = 10 min
- [ ] `ActivateKitCommandHandler` invalidates `cargodry:stats:global` after save
- [ ] `RevokeKitCommandHandler` invalidates `cargodry:stats:global` after save
- [ ] `DailySnapshotJob` invalidates `cargodry:analytics:snapshot` after upsert
- [ ] `IAizenCache` is registered in DI (confirm `AddAizenCache` is called in `Program.cs`)
- [ ] `GET /api/v1/cargodry/admin/products` returns product list correctly
- [ ] Repeated calls to stats endpoint within 2 min do NOT hit the database
