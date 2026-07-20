# CE-3 (backend) — earning columns on the inventory table + renewal-as-revenue

Bring the "money-first" framing into the two tables: the **consignment stock** rows gain earned/potential/sell-through,
and each **renewal** candidate gains its commission. Money math stays on the server (same base as CE-1:
`earning = (ConsignmentPrice ?? RetailPrice) × ProviderCommissionRate`). No response-shape change — new fields on the
existing module DTOs flow through the typed BFF passthrough automatically.

## 1. Inventory list — extend `CargoDryProviderInventoryListItemDto`
Add:
```csharp
public decimal EarnedCommission    { get; init; }  // Σ ProviderShareAmount of attributed sales for this provider+product+batch
public decimal PotentialCommission { get; init; }  // AvailableStock × product earning-per-sale
public decimal SellThroughPct      { get; init; }  // TotalActivated / TotalAllocated × 100 (0 when Allocated = 0)
public string  CurrencyCode        { get; init; } = "USD";
```
Handler `GetProviderInventoryListQueryHandler` (inject `ICargoDryProductRepository` + `ICargoDrySalesAttributionRepository`):
- **Batch-load products** for the page's distinct product codes → get `ProviderCommissionRate`, `ConsignmentPrice`,
  `RetailPrice`, `CurrencyCode`.
- Per row:
  - `PotentialCommission = AvailableStock × round((ConsignmentPrice ?? RetailPrice) × (ProviderCommissionRate ?? 0), 2)`
  - `SellThroughPct = TotalAllocated > 0 ? round(TotalActivated * 100m / TotalAllocated, 1) : 0`
  - `EarnedCommission` = sum of `SalesAttribution.ProviderShareAmount` for (ProviderProfileId, ProductCode, BatchCode,
    `Status != Cancelled`). Add a repo aggregate `SumProviderCommissionByProductBatchAsync(providerProfileId,
    productCode, batchCode, ct)` (SQL SUM) and call it per row (inventory pages are small), or batch-group in one query.
- Reuse the CE-1 formula; prefer a shared domain helper `CargoDryProductEntity.ProviderEarningPerSale()` so the
  formula lives in one place (CE-1's DTO computed field can delegate to it too).

## 2. Renewals — extend `CargoDryRenewalCandidateDto`
Add:
```csharp
public decimal? RenewalCommission { get; init; }  // product earning-per-sale; null when no rate
```
Handler `GetCargoDryRenewalCandidatesQueryHandler` already loads `productsByCode`. In the map, set
`RenewalCommission = product is null ? null : round((product.ConsignmentPrice ?? product.RetailPrice) ×
(product.ProviderCommissionRate ?? 0), 2)`. (The FE sums these for the "total renewal commission" header — no wrapper
needed.)

## 3. BFF
No new endpoint. `GetInventory` / `GetRenewals` already return the module DTOs through the typed remote-call, so the
new fields serialize automatically. Just confirm the BFF references the same Abstraction DTO (it does) and rebuild.

## Verify — run these after the change and paste the output (container + DB level; do not report done until they pass)
Real tables: `sales_attributions`, `provider_inventories`, `sell_through_settlements`. Postgres creds/db from
`docker-compose.yaml`.

1. **Build** — module + BFF compile: `0 Error(s)`.

2. **Rebuild + restart** `cargodry-api` + `bff-marineprovider`, then check boot log has no seed failure:
   ```
   docker compose logs cargodry-api | grep -iE "provider2 sales attributions|failed \(non-fatal\)"
   ```
   Expect the "Seeded ... (3) + settlement (1)." line, no "failed".

3. **DB inputs** (no token needed) — the numbers the handler will compute from:
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "
     SELECT \"AvailableStock\", \"TotalActivated\", \"TotalAllocated\"
       FROM provider_inventories WHERE \"ProviderProfileId\"=100011;               -- expect 2, 4, 7
     SELECT \"ProductCode\", COALESCE(SUM(\"ProviderShareAmount\"),0) AS earned
       FROM sales_attributions WHERE \"ProviderProfileId\"=100011 AND \"Status\"<>5
       GROUP BY \"ProductCode\";                                                    -- expect STANDARD-90 ~90
     SELECT \"ProductCode\", \"ConsignmentPrice\", \"ProviderCommissionRate\"
       FROM products WHERE \"ProductCode\"='STANDARD-90';"                          -- expect 149.99, 0.20
   ```

4. **HTTP smoke** (best-effort — if a provider2 token is handy):
   ```
   # inventory row for provider2:
   GET /api/v1/provider/cargodry/inventory
   #   expect the STANDARD-90 / 202507-CONS-PRV2 row with:
   #   earnedCommission ≈ 90, potentialCommission = 2 × 30 = 60, sellThroughPct = 57.1, currencyCode "USD"
   GET /api/v1/provider/cargodry/renewals?withinDays=90
   #   expect each of the 2 candidates with renewalCommission = 30
   ```
   If no token, steps 1-3 prove the inputs + wiring; the computed HTTP/on-screen values are verified separately on the
   provider portal.

## Acceptance
- Inventory list rows carry `earnedCommission`, `potentialCommission`, `sellThroughPct`, `currencyCode`; renewals
  carry `renewalCommission`. Values for provider2: earned ≈ 90, potential 60, sell-through 57.1; renewal 30 each.
- Build clean; boot log has no seed failure; DB inputs match (2/4/7, earned ~90, rate 0.20).

## Report
`REPORT_BACKEND.md` ("CE-3"): added earned/potential/sell-through to the provider inventory list DTO+handler and
`RenewalCommission` to the renewal candidate DTO+handler (server-computed, same base as CE-1); typed BFF passthrough
carries them. Verified at build + boot-log + DB-input level.
