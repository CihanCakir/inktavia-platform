# BE — Admin Dashboard Charts Aggregation (`GET /dashboard/charts`)

**Goal.** Replace the last three mock charts on the admin dashboard (`/app/dashboard`) with real data:
C1 Revenue & Commission (monthly, 12 buckets), C2 Service Request Volume (by status), C3 Vessel Fleet Status (by status).
Add three module-level aggregation reads, expose them through one new AdminPanel BFF query `GET /dashboard/charts`,
and keep everything additive (no existing endpoint/DTO changed).

**Constraints (project standing rules).**
- Modular monolith, CQRS (Query + Handler + Response), repository pattern already present. Do **not** invent new patterns.
- Cross-module composition happens **only** in the BFF; modules stay pure and self-contained.
- Typed request/response bodies end-to-end (no `object`/`JsonElement`) — see `provider_bff_typed_body` convention.
- Best-effort BFF aggregation: a downstream failure yields an empty series + an `AdminBffWarning`, never a 500.
- **PostgreSQL/Npgsql timestamptz rule:** grouping over a `timestamptz` column throws unless the truncation is done
  in-SQL with a UTC-safe expression. Use EF Core `DateTime.SpecifyKind`/`DateTrunc` carefully — see
  `NPGSQL_TIMESTAMPTZ_RULE`. Prefer computing the month bucket with `date_trunc('month', col AT TIME ZONE 'UTC')`
  translated via a raw/`EF.Functions` expression, or aggregate in memory over a bounded (last-12-months) fetch.
- **DO NOT COMMIT.** Leave changes staged for the owner.

---

## Part A — ServiceRequest module: status breakdown (C2)

Existing: `GetAdminServiceRequestStatsQueryHandler` → `_repository.GetAdminStatsAsync(ct)` returns total/active/critical.
`ServiceRequestRepository` + `IServiceRequestRepository` already exist.

1. **Repository.** Add to `IServiceRequestRepository`:
   ```csharp
   Task<IReadOnlyList<ServiceRequestStatusCount>> GetStatusBreakdownAsync(CancellationToken ct);
   ```
   where `ServiceRequestStatusCount` is a small domain read-model `{ ServiceRequestStatus Status; int Count; }`.
   Implement in `ServiceRequestRepository` with a single `GROUP BY status` (no date column → no timestamptz concern):
   ```csharp
   return await _context.ServiceRequests
       .AsNoTracking()
       .GroupBy(x => x.Status)
       .Select(g => new ServiceRequestStatusCount { Status = g.Key, Count = g.Count() })
       .ToListAsync(ct);
   ```
2. **Query/Handler/Response.** New query `GetAdminServiceRequestStatusBreakdown` under
   `Application/Query/Admin/GetAdminServiceRequestStatusBreakdown/` mirroring the existing stats query.
   Response: `IReadOnlyList<{ string Status (lowercased enum name), int Count }>`.
   Map the enum to the same lowercase key the BFF/FE use (`WaitingForOffer` → `waitingforoffer`).
3. **Controller.** Add `GET /service-requests/stats/status-breakdown` on the SR admin controller (next to the stats endpoint).
4. **Tests.** Repository test: seed SRs across ≥3 statuses, assert grouped counts; handler test: mapping to lowercase keys.

## Part B — Vessel module: status counts (C3)

Mirror the existing `GetVesselCountsByOwnerUserIds` / `GetVesselNamesByIds` batch pattern (added in QA-3).

1. **Repository.** `Task<IReadOnlyList<VesselStatusCount>> GetStatusCountsAsync(CancellationToken ct)` — `GROUP BY Status`.
2. **Query/Handler/Response** `GetVesselStatusCounts` → `IReadOnlyList<{ string Status, int Count }>` (lowercased status key).
3. **Controller.** `GET /vessels/stats/status-counts` (admin-guarded, same authz as other admin vessel reads).
4. **Tests.** Repository grouped-count test + handler mapping test.

## Part C — Payment module: monthly revenue/commission series (C1)

Existing: `GetFinancialSummaryReportQueryHandler` + `PaymentFinanceController` over the append-only financial ledger.
No monthly timeseries exists (the FE finance page aggregates client-side over a 500-row bulk read — do **not** copy that).

1. **Repository.** Add `Task<IReadOnlyList<MonthlyRevenueCommissionPoint>> GetMonthlyRevenueCommissionAsync(int months, CancellationToken ct)`
   returning `{ DateOnly Month; decimal Revenue; decimal Commission; }` for the last `months` (default 12), oldest→newest,
   **including zero-filled gaps** (months with no ledger rows must appear with 0/0 so the chart x-axis is continuous).
   - Revenue = sum of revenue-signed ledger entries; Commission = sum of commission entries — reuse the same
     entry-type/sign classification the financial-summary handler already uses (do not re-derive signs).
   - **timestamptz:** bucket by `date_trunc('month', OccurredAtUtc AT TIME ZONE 'UTC')`. If translating that in LINQ is
     fragile, fetch the last-12-months window (`OccurredAtUtc >= startUtc`) then group **in memory** by
     `new DateOnly(d.Year, d.Month, 1)` — bounded row count makes this safe. Zero-fill the 12 buckets after grouping.
   - All boundary dates computed in **UTC**.
2. **Query/Handler/Response** `GetMonthlyRevenueCommissionReport` → `IReadOnlyList<{ string Month (yyyy-MM), decimal Revenue, decimal Commission }>`.
3. **Controller.** `GET /payment/finance/reports/monthly-revenue-commission?months=12` on `PaymentFinanceController`.
4. **Tests.** Seed ledger across 3 months incl. an empty month; assert 12 buckets, correct sums, zero-fill, UTC boundaries.

## Part D — AdminPanel BFF: `GET /dashboard/charts`

New query `GetDashboardChartsBff` under `Aizen.Bff.AdminPanel.Application/Dashboard/Query/GetDashboardChartsBff/`
(mirror `GetDashboardOverviewBff`). Response `AdminDashboardChartsResponse`:
```csharp
public sealed class AdminDashboardChartsResponse {
  public IReadOnlyList<StatusCountDto> ServiceRequestVolume { get; init; } = [];   // C2
  public IReadOnlyList<StatusCountDto> VesselFleetStatus  { get; init; } = [];    // C3
  public IReadOnlyList<MonthlyRevenueCommissionDto> Revenue { get; init; } = [];  // C1
  public List<AdminBffWarning> Warnings { get; init; } = [];
}
```
- Handler calls the three module endpoints via their typed remote-call interfaces
  (`IServiceRequestRemoteCall`, `IVesselRemoteCall`, `IPaymentFinanceRemoteCall` — add the three new methods to these
  interfaces + Refit clients, hardcoded per-module audience mapper already exists for each).
- Each downstream call is independent and **best-effort**: wrap each in try/catch; on failure append an
  `AdminBffWarning` and leave that series empty. Never throw from the aggregate.
- Controller: `GET /dashboard/charts` next to `/dashboard/overview` (same admin authz).

## Part E — admin-web wiring (after BFF is up)

Repo `inktavia-marine-admin-web`:
1. `endpoints.ts`: `DASHBOARD_CHARTS: '/dashboard/charts'`.
2. `dashboardApi.getCharts()` + `useDashboardChartsQuery()` (mirror `getOverview`/`useDashboardOverviewQuery`).
3. `DashboardPage.tsx`: replace the `revenueData` / `serviceVolumeData` / `fleetStatusData` module constants with the
   query data, exactly like C4 was wired:
   - C1: `data.revenue` → `{ month, revenue, commission }` (month label already `yyyy-MM`; format to short month via i18n if desired).
   - C2: `data.serviceRequestVolume` → map `status` through `serviceRequests:statuses.<status>` for the x-axis label
     (reuse the existing SR namespace; keep the existing `SR_STATUS_VARIANT`/color scheme).
   - C3: `data.vesselFleetStatus` → map `status` through a new `dashboard:vessel.<status>` block (add en+tr keys).
   - Keep per-card loading/empty states (already present).
4. Remove the now-dead mock constants + unused `dashboard:fleet` / `dashboard:srStatus`-for-chart keys if fully replaced.

## Verification
- Backend: all new unit tests green; `dotnet build` 0 errors; each new endpoint returns real data via Postman/curl with an admin token.
- BFF: `GET /dashboard/charts` returns three populated series for seeded data; kill one downstream → that series empty + a warning, HTTP 200.
- admin-web: `tsc --noEmit` + `eslint` clean; `/app/dashboard` shows real revenue/SR-volume/fleet charts, TR labels, no console missing-key.
- **No commit.**
