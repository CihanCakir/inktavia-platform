# CE-4 (backend) — monthly target + progress on the earnings summary

Turn the cockpit's middle block into a real **target + progress**. Per the roadmap MVP, the target is **auto-computed**
(no new table/entity) from the provider's own recent history, and expressed as a **monthly commission (money) target**
to stay consistent with the money-first cockpit. Admin-set targets are post-MVP.

## 1. Extend `CargoDryProviderEarningsDto`
Add:
```csharp
public decimal MonthlyTarget      { get; init; }  // auto-computed monthly commission target
public decimal TargetAchieved     { get; init; }  // = ThisMonthCommission (this month's commission)
public decimal RemainingToTarget  { get; init; }  // max(MonthlyTarget - TargetAchieved, 0)
public decimal ProgressPct        { get; init; }  // clamp(TargetAchieved / MonthlyTarget × 100, 0..100); 0 when target = 0
```
(Keep all existing fields.)

## 2. Auto-target logic — in `GetCargoDryProviderEarningsQueryHandler`
Reuse the existing `ICargoDrySalesAttributionRepository.SumProviderCommissionAsync(pid, fromUtc, toUtc, ct)`:
- Compute commission for each of the **last 3 complete calendar months** (exclude the current month): three
  `[monthStart, monthEnd)` ranges.
- `avg3 = (m1 + m2 + m3) / 3`.
- `MonthlyTarget = Math.Max(Math.Round(avg3 * 1.10m, 0), TargetFloor)` — 10% growth on the trailing average, with a
  floor so new/low-history providers still get a meaningful goal. `const decimal TargetFloor = 150m;` (illustrative,
  in the provider's currency; make it a named constant / config so finance can tune it).
- `TargetAchieved = thisMonthCommission` (already computed).
- `RemainingToTarget = Math.Max(MonthlyTarget - TargetAchieved, 0m)`.
- `ProgressPct = MonthlyTarget > 0 ? Math.Clamp(Math.Round(TargetAchieved * 100m / MonthlyTarget, 1), 0m, 100m) : 0m`.

> Assumption flagged: auto-target is trailing-average × 1.10 with a 150 floor — illustrative; a real deployment sets
> per-provider targets (CE-4 admin override, later). Keep it fair (history-based, achievable).

## 3. BFF
No change — the earnings endpoint already returns `CargoDryProviderEarningsDto` through the typed passthrough; the new
fields serialize automatically. Rebuild bff so its Refit DTO picks them up.

## Verify — run these after the change and paste output (container + DB; do not report done until they pass)
Tables: `sales_attributions`. Creds/db from `docker-compose.yaml`.

1. **Build** — module + BFF: `0 Error(s)`.

2. **Rebuild + restart** `cargodry-api` + `bff-marineprovider`; boot log has no seed failure:
   ```
   docker compose logs cargodry-api | grep -iE "provider2 sales attributions|failed \(non-fatal\)"
   ```

3. **DB inputs** (no token) — this-month vs prior-months commission for provider2:
   ```
   docker compose exec -T postgres psql -U <user> -d <db> -c "
     SELECT date_trunc('month', \"CreatedAtUtc\") AS mon, COALESCE(SUM(\"ProviderShareAmount\"),0) AS commission
       FROM sales_attributions
      WHERE \"ProviderProfileId\"=100011 AND \"Status\"<>5
      GROUP BY 1 ORDER BY 1;"
   ```
   Expect: only the current month has ~90; prior 3 months absent/0 → `avg3 = 0` → `MonthlyTarget = 150` (floor).

4. **HTTP smoke** (best-effort — provider2 token):
   ```
   GET /api/v1/provider/cargodry/earnings
   #   expect: monthlyTarget = 150, targetAchieved = 90 (≈89.99), remainingToTarget ≈ 60, progressPct ≈ 60
   ```
   If no token, steps 1-3 prove the inputs; the computed target/progress are verified on screen.

## Acceptance
- Earnings DTO carries `monthlyTarget`, `targetAchieved`, `remainingToTarget`, `progressPct`.
- Provider2 (this-month 90, no prior history): `monthlyTarget = 150`, `targetAchieved ≈ 90`, `remainingToTarget ≈ 60`,
  `progressPct ≈ 60`.
- Build clean; boot log no seed failure; DB shows only the current month with ~90 commission.

## Report
`REPORT_BACKEND.md` ("CE-4"): added auto-computed monthly commission target + progress to the earnings summary
(trailing-3-month average × 1.10, floor 150; achieved = this-month commission). No new table. Verified at build +
boot-log + DB-input level.
