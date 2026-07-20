# CE-5 (backend) — commission trend + top-earning products (provider-scoped)

Two small read-only, provider-scoped endpoints that feed the CE-5 frontend (sell-through/commission **trend** mini
chart + **"en çok kazandıran"** product badges). **No new repo methods needed** — reuse the aggregates added in
CE-2/CE-3. Follow conventions: module query + provider controller, BFF CQRS (`CargoDry/Query/...Bff`), typed
`AizenApiResponse<T>` + `[ProducesResponseType]`, DTOs in module Abstraction, one-class-per-file. (The product
earning **calculator** on the Ürünler cards is FE-only — it uses the existing `ProviderEarningPerSale`; no backend.)

## Endpoint 1 — commission trend
`GET /api/v1/provider/cargodry/earnings/trend?months=6` (default 6, clamp 1..24).
DTO `Abstraction/Dto/CargoDryEarningsTrendPointDto.cs`:
```csharp
public sealed class CargoDryEarningsTrendPointDto
{
    public string  Month        { get; init; } = default!;   // "yyyy-MM" (UTC)
    public decimal Commission   { get; init; }                // provider commission earned that month
    public string  CurrencyCode { get; init; } = "USD";
}
```
Module query `GetCargoDryProviderCommissionTrend { long ProviderProfileId; int Months }` + handler: for each of the
last `Months` calendar months (oldest→newest, **including** the current month), call the existing
`ICargoDrySalesAttributionRepository.SumProviderCommissionAsync(pid, monthStart, monthEnd, ct)` and emit a point.
Returns `List<CargoDryEarningsTrendPointDto>`.

## Endpoint 2 — top-earning products
`GET /api/v1/provider/cargodry/products/performance`.
DTO `Abstraction/Dto/CargoDryProductPerformanceDto.cs`:
```csharp
public sealed class CargoDryProductPerformanceDto
{
    public string  ProductCode      { get; init; } = default!;
    public decimal EarnedCommission { get; init; }
    public string  CurrencyCode     { get; init; } = "USD";
}
```
Module query `GetCargoDryProviderProductPerformance { long ProviderProfileId }` + handler: call the existing
`SumProviderCommissionByProductBatchAsync(pid, ct)` (returns `Dictionary<(ProductCode, BatchCode), decimal>`), **group
by `ProductCode`** summing across batches, sort by earned commission **descending**, return the list. (FE badges the
top entry.)

## Controller + BFF
- Module `CargoDryProviderController`: add `GET earnings/trend` (query params `months`) and `GET products/performance`,
  both resolving `ResolveProviderProfileId()` and returning `SetResponse(...)` (typed + PRT).
- BFF `ICargoDryRemoteCall`: add `GetEarningsTrend([Query] int months, ...)` → `AizenApiResponse<List<CargoDryEarningsTrendPointDto>>`
  and `GetProductPerformance(...)` → `AizenApiResponse<List<CargoDryProductPerformanceDto>>`.
- BFF `CargoDry/Query/GetCargoDryTrendBff` + `GetCargoDryProductPerformanceBff` (Query + Handler each, resolver →
  `.Body`); `CargoDryController`: `GET earnings/trend` + `GET products/performance`, typed + PRT.

## Verify — run after the change and paste output (container + DB; do not report done until all pass)
Tables: `sales_attributions`. Creds/db from `docker-compose.yaml`.

1. **Build** — module + BFF: `0 Error(s)`.
2. **Rebuild + restart** `cargodry-api` + `bff-marineprovider`; boot log clean:
   ```
   docker compose logs cargodry-api | grep -iE "provider2 sales attributions|failed \(non-fatal\)"
   ```
3. **DB inputs** (no token):
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "
     -- trend source: commission by month
     SELECT to_char(date_trunc('month', \"CreatedAtUtc\"),'YYYY-MM') AS mon,
            COALESCE(SUM(\"ProviderShareAmount\"),0) AS commission
       FROM sales_attributions WHERE \"ProviderProfileId\"=100011 AND \"Status\"<>5
      GROUP BY 1 ORDER BY 1;                                   -- expect current month ≈ 90, others absent
     -- product performance source: commission by product
     SELECT \"ProductCode\", COALESCE(SUM(\"ProviderShareAmount\"),0) AS earned
       FROM sales_attributions WHERE \"ProviderProfileId\"=100011 AND \"Status\"<>5
      GROUP BY 1 ORDER BY 2 DESC;"                             -- expect STANDARD-90 ≈ 90
   ```
4. **HTTP smoke** (best-effort — provider2 token):
   ```
   GET /api/v1/provider/cargodry/earnings/trend?months=6
   #   expect 6 points, current month commission ≈ 90, earlier months 0
   GET /api/v1/provider/cargodry/products/performance
   #   expect [{ productCode: "STANDARD-90", earnedCommission ≈ 90 }, ...]
   ```
   If no token, steps 1-3 prove the inputs + wiring; the HTTP/chart is verified on screen.

## Acceptance
- `earnings/trend` returns `Months` monthly points (current ≈ 90, prior 0); `products/performance` returns products
  sorted by earned commission desc (STANDARD-90 top ≈ 90). Both provider-scoped, typed + PRT.
- Build clean; boot log no seed failure; DB month/product aggregates match.

## Report
`REPORT_BACKEND.md` ("CE-5"): added provider commission-trend + product-performance endpoints (reusing existing
attribution aggregates), module query + BFF CQRS, typed + PRT, DTOs in Abstraction. Verified at build + boot-log + DB.
