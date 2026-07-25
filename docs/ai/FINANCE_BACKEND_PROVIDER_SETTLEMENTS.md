# Provider Finance — Settlements List + Payout Summary — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.CargoDry`. The provider portal's "Finance" page needs to show the
> provider's CargoDry consignment **payouts / settlements**: what's pending, scheduled, and paid — the settlement-level
> view that closes the earnings loop (CE-1..6 showed "what I earned"; this shows "what gets paid"). The tier bonus
> (CE-6a-(b)) already flows into `ProviderPayoutAmount`, so payouts reflect it automatically.
>
> **Scope = read-only, provider-scoped, reuses existing repo methods.** No new tables, no settlement mutation.

## Verified anchors
- Entity `CargoDrySellThroughSettlementEntity`: `SettlementCode`, `ProviderProfileId`, `ProductCode`,
  `PeriodStartUtc`, `PeriodEndUtc`, `TotalCommissionAmount`, `ProviderPayoutAmount`, `CurrencyCode`,
  `Status` (`CargoDrySellThroughSettlementStatus`: Pending=1, ReadyForSettlement=2, Scheduled=3, Settled=4,
  Cancelled=5, Disputed=6), `ScheduledSettlementDate?`, `SettledAtUtc?`, `PayoutCompletedAtUtc?`.
- Repo `ICargoDrySellThroughSettlementRepository`:
  - `GetPagedAsync(long? providerProfileId, long? agreementId, string? productCode, status?, DateTime? periodFrom,
    DateTime? periodTo, string? search, int skip, int take, ct)` → `(Items, Total)` — **already provider-filterable**.
  - `SumProviderPayoutByStatusAsync(long providerProfileId, IEnumerable<status> statuses, ct)` → decimal.
- Provider controller `CargoDryProviderController` ([Route("api/v1/cargodry/provider")]), `ResolveProviderProfileId()`
  → `_cqrs.ProcessAsync<T>(...)` → `SetResponse(...)`. BFF provider routes `/api/v1/provider/cargodry/...`.
- Convention: query/handler separate files; DTOs in `CargoDry.Abstraction/Dto/`; typed `AizenApiResponse<T>`.

---

## 1) DTOs (Abstraction/Dto)
```csharp
public sealed class CargoDryProviderSettlementDto
{
    public string  SettlementCode        { get; init; } = default!;
    public string  ProductCode           { get; init; } = default!;
    public DateTime PeriodStartUtc        { get; init; }
    public DateTime PeriodEndUtc          { get; init; }
    public decimal TotalCommissionAmount  { get; init; }
    public decimal ProviderPayoutAmount   { get; init; }   // reflects tier bonus (CE-6a-b)
    public string  CurrencyCode           { get; init; } = "USD";
    public int     Status                 { get; init; }   // CargoDrySellThroughSettlementStatus (numeric)
    public DateTimeOffset? ScheduledSettlementDate { get; init; }
    public DateTimeOffset? SettledAtUtc            { get; init; }
    public DateTimeOffset? PayoutCompletedAtUtc    { get; init; }
}

public sealed class CargoDryProviderSettlementPagedResultDto
{
    public List<CargoDryProviderSettlementDto> Items { get; init; } = [];
    public int Total    { get; init; }
    public int Page     { get; init; }
    public int PageSize { get; init; }
}

public sealed class CargoDryProviderPayoutSummaryDto
{
    public decimal PendingPayout      { get; init; }   // Status Pending
    public decimal ReadyPayout        { get; init; }   // Status ReadyForSettlement
    public decimal ScheduledPayout    { get; init; }   // Status Scheduled
    public decimal PaidPayout         { get; init; }   // Status Settled
    public decimal DisputedPayout     { get; init; }   // Status Disputed (optional surface)
    public string  CurrencyCode       { get; init; } = "USD";
    public DateTimeOffset ComputedAtUtc { get; init; }
}
```

## 2) Module queries + handlers (Application/Queries/…)
- `GetCargoDryProviderSettlements` — `{ long ProviderProfileId; int? Status; int Page=1; int PageSize=20 }` →
  `CargoDryProviderSettlementPagedResultDto`. Handler: `skip=(Page-1)*PageSize`; call
  `GetPagedAsync(providerProfileId: pid, null, null, status: (enum?)Status, null, null, null, skip, PageSize, ct)`;
  map entities → DTO; return paged result. Currency from the first item (or "USD").
- `GetCargoDryProviderPayoutSummary` — `{ long ProviderProfileId }` → `CargoDryProviderPayoutSummaryDto`. Handler calls
  `SumProviderPayoutByStatusAsync(pid, [Pending], ct)`, `[ReadyForSettlement]`, `[Scheduled]`, `[Settled]`, `[Disputed]`
  (or one call per bucket) and fills the DTO. Currency from products or "USD".

## 3) Module controller (CargoDryProviderController)
```csharp
[HttpGet("settlements")]
public async Task<AizenApiResponse<CargoDryProviderSettlementPagedResultDto>> GetSettlements(
    [FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
{
    var pid = ResolveProviderProfileId();
    var result = await _cqrs.ProcessAsync<CargoDryProviderSettlementPagedResultDto>(
        new GetCargoDryProviderSettlementsQuery { ProviderProfileId = pid, Status = status, Page = page, PageSize = pageSize }, ct);
    return SetResponse(result);
}

[HttpGet("settlements/summary")]
public async Task<AizenApiResponse<CargoDryProviderPayoutSummaryDto>> GetPayoutSummary(CancellationToken ct = default)
{
    var pid = ResolveProviderProfileId();
    var result = await _cqrs.ProcessAsync<CargoDryProviderPayoutSummaryDto>(
        new GetCargoDryProviderPayoutSummaryQuery { ProviderProfileId = pid }, ct);
    return SetResponse(result);
}
```
Routes: `GET /api/v1/cargodry/provider/settlements`, `.../settlements/summary`.

## 4) Provider BFF
- `ICargoDryRemoteCall` (mirror `GetEarnings`):
  ```csharp
  [AizenRemoteCallGet("/api/v1/cargodry/provider/settlements")]
  Task<AizenApiResponse<CargoDryProviderSettlementPagedResultDto>> GetSettlements([Refit.Query] int? status, [Refit.Query] int page = 1, [Refit.Query] int pageSize = 20);
  [AizenRemoteCallGet("/api/v1/cargodry/provider/settlements/summary")]
  Task<AizenApiResponse<CargoDryProviderPayoutSummaryDto>> GetPayoutSummary();
  ```
- BFF queries `GetCargoDrySettlementsBff` + `GetCargoDryPayoutSummaryBff` (resolver → `.Body`, mirror `GetCargoDryTrendBff`);
  BFF `CargoDryController`: `GET settlements` + `GET settlements/summary`, typed + `[ProducesResponseType]`.
  Routes: `GET /api/v1/provider/cargodry/settlements`, `.../settlements/summary`.
- `cargodry` audience-mapper + remote-call already registered — no new DI.

---

## Verify — run and PASTE output (container + DB; do not report done until all pass)
Table: `sell_through_settlements`. DB: aizen/aizen. provider2 = **100011**.

1. **Build** module + BFF: 0 errors. Rebuild + restart `cargodry-api` + `bff-marineprovider`; clean boot.
2. **DB inputs** (no token) — provider2 settlements + payout-by-status:
   ```
   docker compose exec -T postgres psql -U aizen -d aizen -c "
     SELECT \"Status\", COUNT(*), COALESCE(SUM(\"ProviderPayoutAmount\"),0) AS payout
       FROM sell_through_settlements WHERE \"ProviderProfileId\"=100011 GROUP BY 1 ORDER BY 1;"
   -- earlier CE-2 data seeded 1 settlement (payout 60, Status Pending=1) → summary.pendingPayout≈60
   ```
3. **HTTP smoke** (provider2 token):
   ```
   GET /api/v1/provider/cargodry/settlements?page=1&pageSize=20   # 200, items = provider2 settlements (period/product/payout/status)
   GET /api/v1/provider/cargodry/settlements?status=1             # 200, only Pending
   GET /api/v1/provider/cargodry/settlements/summary             # 200, pendingPayout≈60, paidPayout matches Settled rows
   ```
   Token yoksa step 2 kanıtlar; UI ekranda doğrulanır.

## Acceptance
- `GET /provider/cargodry/settlements` (+status filter) returns the provider's settlements (period, product,
  commission, payout, status, dates); paged; provider-scoped; typed + PRT.
- `GET /provider/cargodry/settlements/summary` returns payout totals by status (pending/ready/scheduled/paid) via
  `SumProviderPayoutByStatusAsync`; matches DB.
- Read-only; no new table; tier bonus already reflected in `ProviderPayoutAmount`. Build clean; DB + HTTP evidence pasted.

## Report
`REPORT_BACKEND.md` ("Provider finance settlements"): provider-scoped settlements list + payout summary endpoints
(reusing `GetPagedAsync` + `SumProviderPayoutByStatusAsync`), DTOs in Abstraction, BFF passthrough, typed + PRT.
Verified at build + DB + token'd GET. FE Finance page consumes these.
