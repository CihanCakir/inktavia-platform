# PROMPT 1 — CargoDry Kit Usage Report Extension

## Scope

Extend the existing reporting infrastructure to add a dedicated **CargoDry Kit Usage Report**.
This touches three layers:

1. **Backend** — `Aizen.Modules.Reporting` (or the admin BFF reporting controller) — new query + export handler
2. **BFF** — `Aizen.AdminPanel.BFF` — new Refit call + endpoint `/api/v1/admin-panel/cargodry/reports`
3. **Frontend** — extend existing `report.types.ts`, `useReports.ts`, `ReportsPage.tsx`

---

## Architecture Rules

- Follow the same patterns as the existing Reporting module (useReports.ts, ReportsPage.tsx).
- BFF namespace: `Aizen.AdminPanel.BFF.CargoDry`
- Backend namespace: follow the existing Reporting module namespace convention.
- Use the Aizen BFF envelope pattern: `{ header: { isSuccess, errorCode }, body: T }`.
- All new BFF endpoints go under `/api/v1/admin-panel/cargodry/...`.
- Frontend uses `normalizeSuccess` / `normalizeFailure` from `@shared/api/responseNormalizer`.
- All new endpoints must be added to `src/shared/api/endpoints.ts` under the CargoDry section.

---

## STEP 1 — Backend: CargoDry Reporting Query

### 1.1 Report DTO

**File:** `Aizen.Modules.CargoDry.Abstraction/Dto/CargoDryKitUsageReportDto.cs`

```csharp
namespace Aizen.Modules.CargoDry.Abstraction.Dto;

public class CargoDryKitUsageReportDto
{
    // Summary row
    public int    TotalKits             { get; set; }
    public int    ActivatedKits         { get; set; }
    public int    ExpiredKits           { get; set; }
    public int    RevokedKits           { get; set; }
    public int    RenewedKits           { get; set; }
    public double AverageEfficiencyPct  { get; set; }  // avg of EfficiencyPercent at export time
    public double RenewalRatePct        { get; set; }  // RenewedKits / ActivatedKits * 100
    public double AvgActiveDaysAtExpiry { get; set; }  // avg (ExpiresAt - ActivatedAt).TotalDays for expired kits

    // Per-product breakdown
    public List<CargoDryProductUsageRow> ByProduct { get; set; } = [];

    // Per-batch breakdown
    public List<CargoDryBatchUsageRow> ByBatch { get; set; } = [];

    // 30-day daily activation time series
    public List<CargoDryDailyActivationPoint> DailyActivations { get; set; } = [];

    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset? DateFrom   { get; set; }
    public DateTimeOffset? DateTo     { get; set; }
}

public class CargoDryProductUsageRow
{
    public string ProductCode        { get; set; } = "";
    public string ProductName        { get; set; } = "";
    public int    TotalKits          { get; set; }
    public int    ActivatedKits      { get; set; }
    public int    ExpiredKits        { get; set; }
    public int    RenewedKits        { get; set; }
    public double AvgEfficiencyPct   { get; set; }
}

public class CargoDryBatchUsageRow
{
    public string BatchCode          { get; set; } = "";
    public string ProductCode        { get; set; } = "";
    public int    TotalKits          { get; set; }
    public int    ActivatedKits      { get; set; }
    public int    ExpiredKits        { get; set; }
    public DateTimeOffset CreatedAt  { get; set; }
}

public class CargoDryDailyActivationPoint
{
    public DateOnly Date        { get; set; }
    public int      Activations { get; set; }
    public int      Renewals    { get; set; }
}
```

---

### 1.2 Query + Handler

**File:** `Aizen.Modules.CargoDry.Application/Queries/GetCargoDryUsageReport/GetCargoDryUsageReportQuery.cs`

```csharp
namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryUsageReport;

public record GetCargoDryUsageReportQuery(
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo
) : IRequest<GetCargoDryUsageReportResponse>;

public record GetCargoDryUsageReportResponse(CargoDryKitUsageReportDto Report);
```

**File:** `Aizen.Modules.CargoDry.Application/Queries/GetCargoDryUsageReport/GetCargoDryUsageReportQueryHandler.cs`

```csharp
namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryUsageReport;

public class GetCargoDryUsageReportQueryHandler
    : IRequestHandler<GetCargoDryUsageReportQuery, GetCargoDryUsageReportResponse>
{
    private readonly ICargoDryKitRepository _kits;
    private readonly ICargoDryBatchRepository _batches;

    public GetCargoDryUsageReportQueryHandler(
        ICargoDryKitRepository kits,
        ICargoDryBatchRepository batches)
    {
        _kits    = kits;
        _batches = batches;
    }

    public async Task<GetCargoDryUsageReportResponse> Handle(
        GetCargoDryUsageReportQuery request,
        CancellationToken cancellationToken)
    {
        var from = request.DateFrom ?? DateTimeOffset.UtcNow.AddMonths(-3);
        var to   = request.DateTo   ?? DateTimeOffset.UtcNow;

        // Load all kits in date window (manufacture date or activation date within range)
        var allKits = await _kits.GetAllForReportAsync(from, to, cancellationToken);

        // ── Summary ──────────────────────────────────────────────────────────
        var activated = allKits.Where(k => k.Status != CargoDryKitStatus.Available).ToList();
        var expired   = allKits.Where(k => k.Status == CargoDryKitStatus.Expired).ToList();
        var revoked   = allKits.Where(k => k.Status == CargoDryKitStatus.Revoked).ToList();
        var renewed   = allKits.Where(k => k.RenewalCount > 0).ToList();

        // EfficiencyPercent: only meaningful for Activated kits
        var activeKits = allKits.Where(k => k.Status == CargoDryKitStatus.Activated).ToList();
        double avgEff = activeKits.Count > 0
            ? activeKits.Average(k =>
            {
                if (k.ActivatedAt is null || k.ExpiresAt is null) return 0d;
                var total = (k.ExpiresAt.Value - k.ActivatedAt.Value).TotalSeconds;
                if (total <= 0) return 0d;
                var remaining = (k.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalSeconds;
                return Math.Max(0, remaining / total * 100d);
            })
            : 0d;

        double avgActiveDaysAtExpiry = expired.Count > 0
            ? expired
                .Where(k => k.ActivatedAt.HasValue && k.ExpiresAt.HasValue)
                .Average(k => (k.ExpiresAt!.Value - k.ActivatedAt!.Value).TotalDays)
            : 0d;

        // ── By Product ───────────────────────────────────────────────────────
        var byProduct = allKits
            .GroupBy(k => new { k.ProductCode, k.ProductName })
            .Select(g => new CargoDryProductUsageRow
            {
                ProductCode      = g.Key.ProductCode,
                ProductName      = g.Key.ProductName,
                TotalKits        = g.Count(),
                ActivatedKits    = g.Count(k => k.Status != CargoDryKitStatus.Available),
                ExpiredKits      = g.Count(k => k.Status == CargoDryKitStatus.Expired),
                RenewedKits      = g.Count(k => k.RenewalCount > 0),
                AvgEfficiencyPct = g.Where(k => k.Status == CargoDryKitStatus.Activated)
                    .Select(k =>
                    {
                        if (k.ActivatedAt is null || k.ExpiresAt is null) return 0d;
                        var total = (k.ExpiresAt.Value - k.ActivatedAt.Value).TotalSeconds;
                        if (total <= 0) return 0d;
                        var remaining = (k.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalSeconds;
                        return Math.Max(0, remaining / total * 100d);
                    })
                    .DefaultIfEmpty(0d)
                    .Average(),
            })
            .OrderByDescending(r => r.TotalKits)
            .ToList();

        // ── By Batch ─────────────────────────────────────────────────────────
        var batches = await _batches.GetAllForReportAsync(from, to, cancellationToken);
        var byBatch = batches
            .Select(b => new CargoDryBatchUsageRow
            {
                BatchCode    = b.BatchCode,
                ProductCode  = b.ProductCode,
                TotalKits    = allKits.Count(k => k.BatchId == b.Id),
                ActivatedKits= allKits.Count(k => k.BatchId == b.Id && k.Status != CargoDryKitStatus.Available),
                ExpiredKits  = allKits.Count(k => k.BatchId == b.Id && k.Status == CargoDryKitStatus.Expired),
                CreatedAt    = b.CreatedAt,
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        // ── Daily Activations (last 30 days) ─────────────────────────────────
        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
        var dailyActivations = allKits
            .Where(k => k.ActivatedAt >= thirtyDaysAgo)
            .GroupBy(k => DateOnly.FromDateTime(k.ActivatedAt!.Value.UtcDateTime))
            .Select(g => new CargoDryDailyActivationPoint
            {
                Date        = g.Key,
                Activations = g.Count(),
                Renewals    = g.Count(k => k.RenewalCount > 0),
            })
            .OrderBy(p => p.Date)
            .ToList();

        var report = new CargoDryKitUsageReportDto
        {
            TotalKits             = allKits.Count,
            ActivatedKits         = activated.Count,
            ExpiredKits           = expired.Count,
            RevokedKits           = revoked.Count,
            RenewedKits           = renewed.Count,
            AverageEfficiencyPct  = Math.Round(avgEff, 1),
            RenewalRatePct        = activated.Count > 0
                ? Math.Round(renewed.Count / (double)activated.Count * 100d, 1)
                : 0d,
            AvgActiveDaysAtExpiry = Math.Round(avgActiveDaysAtExpiry, 1),
            ByProduct             = byProduct,
            ByBatch               = byBatch,
            DailyActivations      = dailyActivations,
            GeneratedAt           = DateTimeOffset.UtcNow,
            DateFrom              = request.DateFrom,
            DateTo                = request.DateTo,
        };

        return new GetCargoDryUsageReportResponse(report);
    }
}
```

---

### 1.3 Repository Extension

Add to `ICargoDryKitRepository`:

```csharp
Task<List<CargoDryKit>> GetAllForReportAsync(
    DateTimeOffset from,
    DateTimeOffset to,
    CancellationToken cancellationToken = default);
```

Add to `ICargoDryBatchRepository`:

```csharp
Task<List<CargoDryBatch>> GetAllForReportAsync(
    DateTimeOffset from,
    DateTimeOffset to,
    CancellationToken cancellationToken = default);
```

Implementation in `CargoDryKitRepository`:

```csharp
public async Task<List<CargoDryKit>> GetAllForReportAsync(
    DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
{
    return await _context.Kits
        .AsNoTracking()
        .Where(k => k.ManufacturedAt >= from && k.ManufacturedAt <= to
                 || (k.ActivatedAt.HasValue && k.ActivatedAt >= from && k.ActivatedAt <= to))
        .ToListAsync(ct);
}
```

---

### 1.4 Admin Controller Endpoint

Add to `CargoDryAdminController` (in `Aizen.Modules.CargoDry.Api`):

```csharp
/// <summary>GET /cargodry/admin/reports/usage</summary>
[HttpGet("admin/reports/usage")]
public async Task<IActionResult> GetUsageReport(
    [FromQuery] DateTimeOffset? dateFrom,
    [FromQuery] DateTimeOffset? dateTo,
    CancellationToken cancellationToken)
{
    var result = await _mediator.Send(
        new GetCargoDryUsageReportQuery(dateFrom, dateTo), cancellationToken);
    return Ok(result.Report);
}

/// <summary>GET /cargodry/admin/reports/usage/export?format=csv|xlsx</summary>
[HttpGet("admin/reports/usage/export")]
public async Task<IActionResult> ExportUsageReport(
    [FromQuery] string format,
    [FromQuery] DateTimeOffset? dateFrom,
    [FromQuery] DateTimeOffset? dateTo,
    CancellationToken cancellationToken)
{
    // For MVP: trigger async export job, return 202 + jobId
    // Future: stream direct CSV/XLSX
    var result = await _mediator.Send(
        new GetCargoDryUsageReportQuery(dateFrom, dateTo), cancellationToken);

    if (format?.ToLower() == "csv")
    {
        var csv = BuildCsv(result.Report);
        return File(
            System.Text.Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"cargodry-kit-usage-{DateTimeOffset.UtcNow:yyyyMMdd}.csv");
    }

    return Ok(result.Report); // fallback: return JSON
}

private static string BuildCsv(CargoDryKitUsageReportDto r)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("ProductCode,ProductName,TotalKits,ActivatedKits,ExpiredKits,RenewedKits,AvgEfficiencyPct");
    foreach (var row in r.ByProduct)
        sb.AppendLine($"{row.ProductCode},{row.ProductName},{row.TotalKits},{row.ActivatedKits}," +
                      $"{row.ExpiredKits},{row.RenewedKits},{row.AvgEfficiencyPct:F1}");
    return sb.ToString();
}
```

---

## STEP 2 — BFF: Remote Call + Endpoint

### 2.1 BFF DTOs

**File:** `Aizen.AdminPanel.BFF.CargoDry/Dto/CargoDryReportBffDto.cs`

```csharp
namespace Aizen.AdminPanel.BFF.CargoDry.Dto;

public class CargoDryKitUsageReportBffDto
{
    public int    TotalKits             { get; set; }
    public int    ActivatedKits         { get; set; }
    public int    ExpiredKits           { get; set; }
    public int    RevokedKits           { get; set; }
    public int    RenewedKits           { get; set; }
    public double AverageEfficiencyPct  { get; set; }
    public double RenewalRatePct        { get; set; }
    public double AvgActiveDaysAtExpiry { get; set; }
    public List<CargoDryProductUsageRowBffDto> ByProduct       { get; set; } = [];
    public List<CargoDryBatchUsageRowBffDto>   ByBatch         { get; set; } = [];
    public List<CargoDryDailyActivationBffDto> DailyActivations{ get; set; } = [];
    public DateTimeOffset GeneratedAt { get; set; }
}

public class CargoDryProductUsageRowBffDto
{
    public string ProductCode        { get; set; } = "";
    public string ProductName        { get; set; } = "";
    public int    TotalKits          { get; set; }
    public int    ActivatedKits      { get; set; }
    public int    ExpiredKits        { get; set; }
    public int    RenewedKits        { get; set; }
    public double AvgEfficiencyPct   { get; set; }
}

public class CargoDryBatchUsageRowBffDto
{
    public string BatchCode      { get; set; } = "";
    public string ProductCode    { get; set; } = "";
    public int    TotalKits      { get; set; }
    public int    ActivatedKits  { get; set; }
    public int    ExpiredKits    { get; set; }
    public string CreatedAt      { get; set; } = "";
}

public class CargoDryDailyActivationBffDto
{
    public string Date        { get; set; } = ""; // ISO date "2025-01-15"
    public int    Activations { get; set; }
    public int    Renewals    { get; set; }
}
```

---

### 2.2 Refit Extension

Add to `IAdminCargoDryBffRemoteCall`:

```csharp
[Get("/cargodry/admin/reports/usage")]
Task<CargoDryKitUsageReportDto> GetUsageReportAsync(
    [AliasAs("dateFrom")] DateTimeOffset? dateFrom,
    [AliasAs("dateTo")]   DateTimeOffset? dateTo,
    CancellationToken cancellationToken = default);

[Get("/cargodry/admin/reports/usage/export")]
Task<HttpResponseMessage> ExportUsageReportAsync(
    [AliasAs("format")]   string format,
    [AliasAs("dateFrom")] DateTimeOffset? dateFrom,
    [AliasAs("dateTo")]   DateTimeOffset? dateTo,
    CancellationToken cancellationToken = default);
```

---

### 2.3 BFF Query Handler

**File:** `Aizen.AdminPanel.BFF.CargoDry/Queries/GetCargoDryUsageReport/GetCargoDryUsageReportBffQuery.cs`

```csharp
namespace Aizen.AdminPanel.BFF.CargoDry.Queries.GetCargoDryUsageReport;

public record GetCargoDryUsageReportBffQuery(
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo
) : IRequest<GetCargoDryUsageReportBffResponse>;

public record GetCargoDryUsageReportBffResponse(CargoDryKitUsageReportBffDto Report);
```

**Handler:** Call remote, map field-for-field to BFF DTO (1:1 shape, no business logic in BFF):

```csharp
public async Task<GetCargoDryUsageReportBffResponse> Handle(
    GetCargoDryUsageReportBffQuery request,
    CancellationToken cancellationToken)
{
    var raw = await _remote.GetUsageReportAsync(request.DateFrom, request.DateTo, cancellationToken);

    var dto = new CargoDryKitUsageReportBffDto
    {
        TotalKits             = raw.TotalKits,
        ActivatedKits         = raw.ActivatedKits,
        ExpiredKits           = raw.ExpiredKits,
        RevokedKits           = raw.RevokedKits,
        RenewedKits           = raw.RenewedKits,
        AverageEfficiencyPct  = raw.AverageEfficiencyPct,
        RenewalRatePct        = raw.RenewalRatePct,
        AvgActiveDaysAtExpiry = raw.AvgActiveDaysAtExpiry,
        ByProduct             = raw.ByProduct.Select(p => new CargoDryProductUsageRowBffDto
        {
            ProductCode      = p.ProductCode,
            ProductName      = p.ProductName,
            TotalKits        = p.TotalKits,
            ActivatedKits    = p.ActivatedKits,
            ExpiredKits      = p.ExpiredKits,
            RenewedKits      = p.RenewedKits,
            AvgEfficiencyPct = p.AvgEfficiencyPct,
        }).ToList(),
        ByBatch = raw.ByBatch.Select(b => new CargoDryBatchUsageRowBffDto
        {
            BatchCode    = b.BatchCode,
            ProductCode  = b.ProductCode,
            TotalKits    = b.TotalKits,
            ActivatedKits= b.ActivatedKits,
            ExpiredKits  = b.ExpiredKits,
            CreatedAt    = b.CreatedAt.ToString("O"),
        }).ToList(),
        DailyActivations = raw.DailyActivations.Select(d => new CargoDryDailyActivationBffDto
        {
            Date        = d.Date.ToString("yyyy-MM-dd"),
            Activations = d.Activations,
            Renewals    = d.Renewals,
        }).ToList(),
        GeneratedAt = raw.GeneratedAt,
    };

    return new GetCargoDryUsageReportBffResponse(dto);
}
```

---

### 2.4 BFF Controller

**File:** `Aizen.AdminPanel.BFF.CargoDry/Controllers/AdminCargoDryController.cs` — add endpoint:

```csharp
/// <summary>GET /api/v1/admin-panel/cargodry/reports/usage</summary>
[HttpGet("reports/usage")]
public async Task<IActionResult> GetUsageReport(
    [FromQuery] DateTimeOffset? dateFrom,
    [FromQuery] DateTimeOffset? dateTo,
    CancellationToken cancellationToken)
{
    var result = await _mediator.Send(
        new GetCargoDryUsageReportBffQuery(dateFrom, dateTo), cancellationToken);
    return Ok(AizenResponse.Success(result.Report));
}

/// <summary>GET /api/v1/admin-panel/cargodry/reports/usage/export?format=csv</summary>
[HttpGet("reports/usage/export")]
public async Task<IActionResult> ExportUsageReport(
    [FromQuery] string format,
    [FromQuery] DateTimeOffset? dateFrom,
    [FromQuery] DateTimeOffset? dateTo,
    CancellationToken cancellationToken)
{
    var upstream = await _remote.ExportUsageReportAsync(format, dateFrom, dateTo, cancellationToken);
    var bytes    = await upstream.Content.ReadAsByteArrayAsync(cancellationToken);
    var ct       = upstream.Content.Headers.ContentType?.ToString() ?? "text/csv";
    return File(bytes, ct, $"cargodry-kit-usage-{DateTimeOffset.UtcNow:yyyyMMdd}.{format}");
}
```

---

## STEP 3 — Frontend

### 3.1 Add ReportType + Endpoints

**File:** `src/shared/api/types/report.types.ts`

Add to `ReportType` union:

```typescript
export type ReportType =
  | 'vessel-activity'
  | 'service-requests'
  | 'user-stats'
  | 'reference-data'
  | 'payment-summary'
  | 'commission-summary'
  | 'inventory-snapshot'
  | 'cargodry-kit-usage'   // ← ADD
```

Add new DTO types:

```typescript
export interface CargoDryProductUsageRowDto {
  productCode:      string
  productName:      string
  totalKits:        number
  activatedKits:    number
  expiredKits:      number
  renewedKits:      number
  avgEfficiencyPct: number
}

export interface CargoDryBatchUsageRowDto {
  batchCode:     string
  productCode:   string
  totalKits:     number
  activatedKits: number
  expiredKits:   number
  createdAt:     string
}

export interface CargoDryDailyActivationDto {
  date:        string  // "2025-01-15"
  activations: number
  renewals:    number
}

export interface CargoDryKitUsageReportDto {
  totalKits:             number
  activatedKits:         number
  expiredKits:           number
  revokedKits:           number
  renewedKits:           number
  averageEfficiencyPct:  number
  renewalRatePct:        number
  avgActiveDaysAtExpiry: number
  byProduct:             CargoDryProductUsageRowDto[]
  byBatch:               CargoDryBatchUsageRowDto[]
  dailyActivations:      CargoDryDailyActivationDto[]
  generatedAt:           string
}
```

**File:** `src/shared/api/endpoints.ts` — add to CargoDry section:

```typescript
CARGODRY_REPORT_USAGE:        '/cargodry/reports/usage',
CARGODRY_REPORT_USAGE_EXPORT: (format: string) => `/cargodry/reports/usage/export?format=${format}`,
```

---

### 3.2 Hook: useCargoDryUsageReport

**New file:** `src/features/cargodry/hooks/useCargoDryUsageReport.ts`

```typescript
import { useQuery } from '@tanstack/react-query'
import httpClient from '@shared/api/httpClient'
import { ENDPOINTS } from '@shared/api/endpoints'
import { normalizeSuccess, normalizeFailure } from '@shared/api/responseNormalizer'
import type { CargoDryKitUsageReportDto } from '@shared/api/types/report.types'

export function useCargoDryUsageReport(params?: { dateFrom?: string; dateTo?: string }) {
  return useQuery({
    queryKey: ['cargodry', 'report', 'usage', params],
    queryFn: async () => {
      try {
        const r = await httpClient.get(ENDPOINTS.CARGODRY_REPORT_USAGE, { params })
        const result = normalizeSuccess<CargoDryKitUsageReportDto>(r)
        return result.ok ? result.data : null
      } catch (e) {
        normalizeFailure(e)
        return null
      }
    },
    staleTime: 120_000,
    enabled: true,
  })
}
```

---

### 3.3 ReportsPage Extension

**File:** `src/pages/app/ReportsPage.tsx` — add to `REPORT_DEFINITIONS`:

```typescript
{
  type: 'cargodry-kit-usage',
  label: 'CargoDry Kit Usage',
  description: 'Kit lifecycle metrics: activation rates, efficiency distribution, renewal trends',
  icon: 'water_drop',
  formats: ['csv', 'xlsx'],
},
```

Also add a KPI row extension after the existing 4 KPI cards. Import and use `useCargoDryStatsQuery`:

```typescript
import { useCargoDryStatsQuery } from '@features/cargodry/hooks/useCargoDryStatsQuery'

// Inside component:
const { data: cargoDryStats } = useCargoDryStatsQuery()

// After existing KPI grid, add a CargoDry KPI row:
{cargoDryStats && (
  <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-gutter mb-gutter">
    <MetricCard
      label="Active Kits"
      value={cargoDryStats.activeKits}
      icon="water_drop"
    />
    <MetricCard
      label="Avg Efficiency"
      value={`${cargoDryStats.renewalRatePercent.toFixed(1)}%`}
      icon="trending_up"
    />
    <MetricCard
      label="Expiring (30d)"
      value={cargoDryStats.expiringKits}
      icon="timer"
    />
    <MetricCard
      label="Today Activations"
      value={cargoDryStats.todayActivations}
      icon="bolt"
    />
  </div>
)}
```

---

## STEP 4 — Validation

- [ ] Backend: `dotnet build` — no errors in CargoDry.Application and CargoDry.Api projects
- [ ] Backend: GET `/cargodry/admin/reports/usage` returns valid JSON with `byProduct`, `byBatch`, `dailyActivations`
- [ ] Backend: GET `/cargodry/admin/reports/usage/export?format=csv` returns downloadable CSV file
- [ ] BFF: GET `/api/v1/admin-panel/cargodry/reports/usage` returns Aizen envelope
- [ ] Frontend: `tsc --noEmit` — zero errors
- [ ] Frontend: `ReportsPage` shows new "CargoDry Kit Usage" card with CSV + XLSX download buttons
- [ ] Frontend: Export buttons trigger download (check Network tab for 200 response)
- [ ] ReportType union includes `'cargodry-kit-usage'` — no type error in REPORT_DEFINITIONS array

---

## Key Constraints

| Concern | Rule |
|---|---|
| EfficiencyPercent | Computed on-the-fly — NOT persisted in DB, NOT mapped in EF |
| Date filtering | Use `ManufacturedAt` OR `ActivatedAt` — kit may not be activated yet |
| BFF passthrough | No business logic in BFF; calculation happens in Application layer |
| CSV format | Header row + per-product rows; no Excel dependency for CSV export |
| Frontend query key | `['cargodry', 'report', 'usage', params]` — separate from kit list cache |
