# PROMPT 2 — CargoDry Analytics Extension

## Scope

Extend the analytics layer with CargoDry kit-level metrics:

1. **BFF** — new query + endpoint `/api/v1/admin-panel/cargodry/analytics`
2. **Frontend hook** — `useCargoDryAnalytics.ts` (separate from the fleet analytics hook)
3. **Frontend page** — Add a **CargoDry** tab to `AnalyticsChartPage.tsx` with 4 charts:
   - Kit Status Distribution (pie/donut)
   - Daily Activations (line chart, 30d)
   - Efficiency Distribution (bar chart, buckets)
   - Product Mix (bar chart, active kits per product)

---

## Architecture Rules

- Do NOT modify `useAnalyticsDashboard` or `AnalyticsDashboardDto` — those are fleet-level.
- Create a fully separate `useCargoDryAnalytics` hook with its own query key and DTO.
- BFF analytics endpoint is under `/api/v1/admin-panel/cargodry/analytics` (not under general `/admin/analytics`).
- Chart data must be ready to pass directly to Recharts components.
- No placeholder data — use real BFF response; show `LoadingSkeleton` while loading.
- Follow the `DashboardCard` + Recharts pattern already used in `AnalyticsChartPage`.

---

## STEP 1 — BFF: Analytics DTO + Endpoint

### 1.1 BFF DTO

**File:** `Aizen.AdminPanel.BFF.CargoDry/Dto/CargoDryAnalyticsBffDto.cs`

```csharp
namespace Aizen.AdminPanel.BFF.CargoDry.Dto;

/// <summary>
/// Pre-shaped for Recharts. All series values are chart-ready.
/// </summary>
public class CargoDryAnalyticsBffDto
{
    // ── Kit Status Distribution ───────────────────────────────────────────────
    // Used as PieChart data
    public List<CargoDryStatusSliceDto> StatusDistribution { get; set; } = [];

    // ── Daily Activations — 30-day time series ────────────────────────────────
    // Used as LineChart data
    public List<CargoDryDailyActivationBffDto> DailyActivations { get; set; } = [];

    // ── Efficiency Distribution — buckets ─────────────────────────────────────
    // Buckets: 0-25%, 25-50%, 50-75%, 75-100%
    // Used as BarChart data
    public List<CargoDryEfficiencyBucketDto> EfficiencyBuckets { get; set; } = [];

    // ── Product Mix — active kits per product ─────────────────────────────────
    // Used as BarChart data
    public List<CargoDryProductMixDto> ProductMix { get; set; } = [];

    // ── KPI snapshot ─────────────────────────────────────────────────────────
    public int    TotalKits            { get; set; }
    public int    ActiveKits           { get; set; }
    public double AvgEfficiencyPct     { get; set; }
    public double RenewalRatePct       { get; set; }
    public int    ExpiringNext30Days   { get; set; }

    public DateTimeOffset ComputedAt { get; set; }
}

public class CargoDryStatusSliceDto
{
    public string Name  { get; set; } = "";   // "Available", "Activated", "Expired" …
    public int    Value { get; set; }
    public string Color { get; set; } = "";   // HEX, assigned by BFF for consistent palette
}

public class CargoDryDailyActivationBffDto
{
    public string Date        { get; set; } = ""; // "2025-01-15"
    public int    Activations { get; set; }
    public int    Renewals    { get; set; }
}

public class CargoDryEfficiencyBucketDto
{
    public string Bucket { get; set; } = ""; // "0–25%", "25–50%", "50–75%", "75–100%"
    public int    Count  { get; set; }
}

public class CargoDryProductMixDto
{
    public string ProductCode  { get; set; } = "";
    public string ProductName  { get; set; } = "";
    public int    ActiveKits   { get; set; }
    public int    TotalKits    { get; set; }
}
```

---

### 1.2 Backend Query (CargoDry.Application)

**File:** `Aizen.Modules.CargoDry.Application/Queries/GetCargoDryAnalytics/GetCargoDryAnalyticsQuery.cs`

```csharp
namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryAnalytics;

public record GetCargoDryAnalyticsQuery : IRequest<GetCargoDryAnalyticsResponse>;

public record GetCargoDryAnalyticsResponse(CargoDryAnalyticsDto Analytics);
```

**File:** `Aizen.Modules.CargoDry.Abstraction/Dto/CargoDryAnalyticsDto.cs`

```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public class CargoDryAnalyticsDto
{
    public Dictionary<string, int>            StatusDistribution  { get; set; } = [];
    public List<CargoDryDailyActivationPoint> DailyActivations    { get; set; } = [];
    public Dictionary<string, int>            EfficiencyBuckets   { get; set; } = [];
    public List<CargoDryProductMixRow>        ProductMix          { get; set; } = [];

    public int    TotalKits          { get; set; }
    public int    ActiveKits         { get; set; }
    public double AvgEfficiencyPct   { get; set; }
    public double RenewalRatePct     { get; set; }
    public int    ExpiringNext30Days { get; set; }
    public DateTimeOffset ComputedAt { get; set; }
}

public class CargoDryProductMixRow
{
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int    ActiveKits  { get; set; }
    public int    TotalKits   { get; set; }
}
```

**Handler:** `GetCargoDryAnalyticsQueryHandler.cs`

```csharp
public class GetCargoDryAnalyticsQueryHandler
    : IRequestHandler<GetCargoDryAnalyticsQuery, GetCargoDryAnalyticsResponse>
{
    private readonly ICargoDryKitRepository _kits;

    public GetCargoDryAnalyticsQueryHandler(ICargoDryKitRepository kits) => _kits = kits;

    public async Task<GetCargoDryAnalyticsResponse> Handle(
        GetCargoDryAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var allKits = await _kits.GetAllAsync(cancellationToken);
        var now     = DateTimeOffset.UtcNow;

        // Status distribution
        var statusDist = allKits
            .GroupBy(k => k.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Daily activations — last 30 days
        var thirtyAgo = now.AddDays(-30);
        var daily = allKits
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

        // Efficiency buckets (only Activated kits)
        var activeKits = allKits.Where(k => k.Status == CargoDryKitStatus.Activated).ToList();
        var buckets    = new Dictionary<string, int>
        {
            ["0–25%"]    = 0,
            ["25–50%"]   = 0,
            ["50–75%"]   = 0,
            ["75–100%"]  = 0,
        };
        foreach (var k in activeKits)
        {
            if (k.ActivatedAt is null || k.ExpiresAt is null) continue;
            var total = (k.ExpiresAt.Value - k.ActivatedAt.Value).TotalSeconds;
            if (total <= 0) continue;
            var remaining = (k.ExpiresAt.Value - now).TotalSeconds;
            var pct = Math.Max(0, remaining / total * 100d);
            var key = pct < 25 ? "0–25%" : pct < 50 ? "25–50%" : pct < 75 ? "50–75%" : "75–100%";
            buckets[key]++;
        }

        // Product mix
        var productMix = allKits
            .GroupBy(k => new { k.ProductCode, k.ProductName })
            .Select(g => new CargoDryProductMixRow
            {
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                ActiveKits  = g.Count(k => k.Status == CargoDryKitStatus.Activated),
                TotalKits   = g.Count(),
            })
            .OrderByDescending(r => r.TotalKits)
            .ToList();

        // KPI
        double avgEff = activeKits.Count > 0
            ? activeKits.Average(k =>
            {
                if (k.ActivatedAt is null || k.ExpiresAt is null) return 0d;
                var total = (k.ExpiresAt.Value - k.ActivatedAt.Value).TotalSeconds;
                if (total <= 0) return 0d;
                var remaining = (k.ExpiresAt.Value - now).TotalSeconds;
                return Math.Max(0, remaining / total * 100d);
            })
            : 0d;

        var activatedTotal = allKits.Count(k => k.Status != CargoDryKitStatus.Available);
        var renewed        = allKits.Count(k => k.RenewalCount > 0);
        var expiring30     = allKits.Count(k =>
            k.Status == CargoDryKitStatus.Activated &&
            k.ExpiresAt.HasValue &&
            k.ExpiresAt.Value <= now.AddDays(30));

        var dto = new CargoDryAnalyticsDto
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

        return new GetCargoDryAnalyticsResponse(dto);
    }
}
```

Add `GetAllAsync` to `ICargoDryKitRepository`:

```csharp
Task<List<CargoDryKit>> GetAllAsync(CancellationToken cancellationToken = default);
```

---

### 1.3 Backend Controller Endpoint

Add to `CargoDryAdminController`:

```csharp
/// <summary>GET /cargodry/admin/analytics</summary>
[HttpGet("admin/analytics")]
public async Task<IActionResult> GetAnalytics(CancellationToken cancellationToken)
{
    var result = await _mediator.Send(new GetCargoDryAnalyticsQuery(), cancellationToken);
    return Ok(result.Analytics);
}
```

---

### 1.4 BFF Query Handler

Add to `IAdminCargoDryBffRemoteCall`:

```csharp
[Get("/cargodry/admin/analytics")]
Task<CargoDryAnalyticsDto> GetAnalyticsAsync(CancellationToken cancellationToken = default);
```

**Handler** maps raw DTO to BFF DTO, assigning colors to status slices:

```csharp
private static readonly Dictionary<string, string> StatusColors = new()
{
    ["Available"]   = "#b9c7e4",
    ["Activated"]   = "#4ade80",
    ["Expired"]     = "#ef4444",
    ["Renewed"]     = "#e9c349",
    ["Revoked"]     = "#f97316",
    ["Lost"]        = "#a855f7",
    ["Transferred"] = "#06b6d4",
};

// Handler mapping:
StatusDistribution = raw.StatusDistribution
    .Select(kv => new CargoDryStatusSliceDto
    {
        Name  = kv.Key,
        Value = kv.Value,
        Color = StatusColors.GetValueOrDefault(kv.Key, "#64748b"),
    })
    .ToList(),
```

BFF Controller endpoint:

```csharp
/// <summary>GET /api/v1/admin-panel/cargodry/analytics</summary>
[HttpGet("analytics")]
public async Task<IActionResult> GetAnalytics(CancellationToken cancellationToken)
{
    var result = await _mediator.Send(new GetCargoDryAnalyticsBffQuery(), cancellationToken);
    return Ok(AizenResponse.Success(result.Analytics));
}
```

---

## STEP 2 — Frontend: Hook + Types

### 2.1 Add Endpoint

**File:** `src/shared/api/endpoints.ts` — add to CargoDry section:

```typescript
CARGODRY_ANALYTICS: '/cargodry/analytics',
```

### 2.2 Add DTO Types

**File:** `src/entities/cargodry/types/cargodry.types.ts` — append:

```typescript
// ── Analytics DTOs ────────────────────────────────────────────────────────────

export interface CargoDryStatusSliceDto {
  name:  string
  value: number
  color: string
}

export interface CargoDryDailyActivationPointDto {
  date:        string
  activations: number
  renewals:    number
}

export interface CargoDryEfficiencyBucketDto {
  bucket: string
  count:  number
}

export interface CargoDryProductMixDto {
  productCode: string
  productName: string
  activeKits:  number
  totalKits:   number
}

export interface CargoDryAnalyticsDto {
  statusDistribution: CargoDryStatusSliceDto[]
  dailyActivations:   CargoDryDailyActivationPointDto[]
  efficiencyBuckets:  CargoDryEfficiencyBucketDto[]
  productMix:         CargoDryProductMixDto[]
  totalKits:          number
  activeKits:         number
  avgEfficiencyPct:   number
  renewalRatePct:     number
  expiringNext30Days: number
  computedAt:         string
}
```

### 2.3 useCargoDryAnalytics Hook

**New file:** `src/features/cargodry/hooks/useCargoDryAnalytics.ts`

```typescript
import { useQuery }      from '@tanstack/react-query'
import httpClient        from '@shared/api/httpClient'
import { ENDPOINTS }     from '@shared/api/endpoints'
import { normalizeSuccess, normalizeFailure } from '@shared/api/responseNormalizer'
import type { CargoDryAnalyticsDto } from '@entities/cargodry/types/cargodry.types'

export function useCargoDryAnalytics() {
  return useQuery({
    queryKey: ['cargodry', 'analytics'],
    queryFn: async () => {
      try {
        const r = await httpClient.get(ENDPOINTS.CARGODRY_ANALYTICS)
        const result = normalizeSuccess<CargoDryAnalyticsDto>(r)
        return result.ok ? result.data : null
      } catch (e) {
        normalizeFailure(e)
        return null
      }
    },
    staleTime: 120_000,
  })
}
```

---

## STEP 3 — Frontend: AnalyticsChartPage CargoDry Tab

### 3.1 Tab State

**File:** `src/pages/app/analytics/AnalyticsChartPage.tsx`

Wrap existing content in a tab switcher. Add a `tab` state:

```typescript
import { useState }            from 'react'
import { useCargoDryAnalytics } from '@features/cargodry/hooks/useCargoDryAnalytics'
import { CargoDryAnalyticsTab } from './components/CargoDryAnalyticsTab'

// Inside component:
const [tab, setTab] = useState<'fleet' | 'cargodry'>('fleet')
const { data: cdAnalytics, isLoading: cdLoading } = useCargoDryAnalytics()
```

Add tab switcher UI after `<PageHeader>`:

```tsx
{/* Tab Switcher */}
<div className="flex rounded-xl border border-outline-variant/30 overflow-hidden w-fit mb-5">
  {(['fleet', 'cargodry'] as const).map((t) => (
    <button
      key={t}
      onClick={() => setTab(t)}
      className={`px-4 py-2 text-label-sm font-medium capitalize transition-colors ${
        tab === t
          ? 'bg-primary text-on-primary'
          : 'text-on-surface-variant hover:bg-surface-container'
      }`}
    >
      {t === 'fleet' ? 'Fleet Analytics' : 'CargoDry Analytics'}
    </button>
  ))}
</div>

{/* Conditional content */}
{tab === 'fleet' ? (
  /* existing grid of 5 DashboardCards unchanged */
  <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
    {/* FleetEfficiencyChart, FuelConsumptionChart, VesselUtilizationChart, SystemHealthChart, FleetDistributionPlaceholder */}
  </div>
) : (
  <CargoDryAnalyticsTab data={cdAnalytics} isLoading={cdLoading} />
)}
```

---

### 3.2 CargoDryAnalyticsTab Component

**New file:** `src/pages/app/analytics/components/CargoDryAnalyticsTab.tsx`

```tsx
import { DashboardCard }   from '@shared/ui/dashboard-card/DashboardCard'
import { LoadingSkeleton } from '@shared/ui/loading-skeleton/LoadingSkeleton'
import { MetricCard }      from '@shared/ui/metric-card/MetricCard'
import {
  PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer,
  LineChart, Line, XAxis, YAxis, CartesianGrid,
  BarChart, Bar,
} from 'recharts'
import type { CargoDryAnalyticsDto } from '@entities/cargodry/types/cargodry.types'

interface Props {
  data:      CargoDryAnalyticsDto | null | undefined
  isLoading: boolean
}

export function CargoDryAnalyticsTab({ data, isLoading }: Props) {
  if (isLoading || !data) {
    return (
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {Array.from({ length: 4 }).map((_, i) => (
          <DashboardCard key={i} title="">
            <LoadingSkeleton lines={5} />
          </DashboardCard>
        ))}
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* KPI row */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-gutter">
        <MetricCard label="Active Kits"       value={data.activeKits}                          icon="water_drop" />
        <MetricCard label="Avg Efficiency"    value={`${data.avgEfficiencyPct.toFixed(1)}%`}   icon="trending_up" />
        <MetricCard label="Renewal Rate"      value={`${data.renewalRatePct.toFixed(1)}%`}     icon="autorenew" />
        <MetricCard label="Expiring (30d)"    value={data.expiringNext30Days}                  icon="timer" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Status Distribution — Pie */}
        <DashboardCard title="Kit Status Distribution" subtitle="Current snapshot">
          <ResponsiveContainer width="100%" height={220}>
            <PieChart>
              <Pie
                data={data.statusDistribution}
                cx="50%"
                cy="50%"
                innerRadius={55}
                outerRadius={85}
                paddingAngle={3}
                dataKey="value"
                nameKey="name"
              >
                {data.statusDistribution.map((entry, i) => (
                  <Cell key={i} fill={entry.color} />
                ))}
              </Pie>
              <Tooltip
                formatter={(value: number, name: string) => [value, name]}
                contentStyle={{ background: 'var(--md-surface-container)', border: 'none', borderRadius: 8 }}
              />
              <Legend
                iconType="circle"
                iconSize={8}
                formatter={(v) => <span className="text-label-sm text-on-surface-variant">{v}</span>}
              />
            </PieChart>
          </ResponsiveContainer>
        </DashboardCard>

        {/* Daily Activations — Line */}
        <DashboardCard title="Daily Activations" subtitle="Last 30 days">
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={data.dailyActivations} margin={{ top: 5, right: 10, left: -20, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--md-outline-variant)" strokeOpacity={0.3} />
              <XAxis
                dataKey="date"
                tick={{ fontSize: 10, fill: 'var(--md-on-surface-variant)' }}
                tickFormatter={(v: string) => v.slice(5)} // "MM-DD"
              />
              <YAxis tick={{ fontSize: 10, fill: 'var(--md-on-surface-variant)' }} />
              <Tooltip
                contentStyle={{ background: 'var(--md-surface-container)', border: 'none', borderRadius: 8 }}
              />
              <Legend iconType="circle" iconSize={8} />
              <Line type="monotone" dataKey="activations" stroke="#4ade80" strokeWidth={2} dot={false} name="Activations" />
              <Line type="monotone" dataKey="renewals"    stroke="#e9c349" strokeWidth={2} dot={false} name="Renewals"    />
            </LineChart>
          </ResponsiveContainer>
        </DashboardCard>

        {/* Efficiency Buckets — Bar */}
        <DashboardCard title="Efficiency Distribution" subtitle="Active kits by remaining lifetime">
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={data.efficiencyBuckets} margin={{ top: 5, right: 10, left: -20, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--md-outline-variant)" strokeOpacity={0.3} />
              <XAxis dataKey="bucket" tick={{ fontSize: 10, fill: 'var(--md-on-surface-variant)' }} />
              <YAxis tick={{ fontSize: 10, fill: 'var(--md-on-surface-variant)' }} />
              <Tooltip contentStyle={{ background: 'var(--md-surface-container)', border: 'none', borderRadius: 8 }} />
              <Bar dataKey="count" name="Kits" radius={[4, 4, 0, 0]}>
                {data.efficiencyBuckets.map((entry, i) => {
                  const colors = ['#ef4444', '#f97316', '#e9c349', '#4ade80']
                  return <Cell key={i} fill={colors[i] ?? '#b9c7e4'} />
                })}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </DashboardCard>

        {/* Product Mix — Bar */}
        <DashboardCard title="Product Mix" subtitle="Active kits per product line">
          <ResponsiveContainer width="100%" height={200}>
            <BarChart
              data={data.productMix}
              layout="vertical"
              margin={{ top: 5, right: 20, left: 20, bottom: 0 }}
            >
              <CartesianGrid strokeDasharray="3 3" stroke="var(--md-outline-variant)" strokeOpacity={0.3} horizontal={false} />
              <XAxis type="number" tick={{ fontSize: 10, fill: 'var(--md-on-surface-variant)' }} />
              <YAxis
                type="category"
                dataKey="productCode"
                tick={{ fontSize: 10, fill: 'var(--md-on-surface-variant)' }}
                width={80}
              />
              <Tooltip
                contentStyle={{ background: 'var(--md-surface-container)', border: 'none', borderRadius: 8 }}
                formatter={(v: number, name: string) => [v, name]}
              />
              <Bar dataKey="activeKits" fill="#4ade80" name="Active"  radius={[0, 4, 4, 0]} />
              <Bar dataKey="totalKits"  fill="#b9c7e4" name="Total"   radius={[0, 4, 4, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </DashboardCard>
      </div>
    </div>
  )
}
```

---

## STEP 4 — Validation

- [ ] Backend: `dotnet build` — no errors in CargoDry.Application
- [ ] Backend: GET `/cargodry/admin/analytics` returns `statusDistribution`, `dailyActivations`, `efficiencyBuckets`, `productMix`
- [ ] BFF: GET `/api/v1/admin-panel/cargodry/analytics` returns Aizen envelope with color-enriched status slices
- [ ] Frontend: `tsc --noEmit` — zero errors
- [ ] Frontend: `AnalyticsChartPage` shows tab switcher with "Fleet Analytics" and "CargoDry Analytics"
- [ ] CargoDry tab renders all 4 charts without console errors
- [ ] Clicking tab switcher preserves Fleet tab content (no data loss)
- [ ] Recharts imports compile — do NOT add new recharts package (already installed)

---

## Key Constraints

| Concern | Rule |
|---|---|
| Separate hook | `useCargoDryAnalytics` — never merge into `useAnalyticsDashboard` |
| EfficiencyPercent | Computed server-side in handler — NOT a DB column |
| Color assignment | Done in BFF mapper, never in frontend types |
| Tab state | Local `useState` in AnalyticsChartPage — no URL state needed for MVP |
| Recharts version | Use the same recharts already imported in existing chart components |
| CSS variables | Chart tooltips use `var(--md-surface-container)` for theme compatibility |
