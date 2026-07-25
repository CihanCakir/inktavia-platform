# CE-6a-(b) — Rebuild, Smoke-Test the Tier Commission Bonus, Diagnose & Fix — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.CargoDry`. CE-6a-(b) (tier-based real commission bonus) was
> implemented and builds clean, but the **DB-level financial behavior was not yet verified**. This prompt: rebuilds
> the container, runs the full smoke test (including a Silver/Gold scenario to prove the differential payout, plus
> immutability and cap), and — **if any check fails or any error appears** — diagnoses from logs, fixes the code,
> rebuilds, and re-runs until all pass. Do NOT report done until every check passes with pasted output.

**Mechanism under test (from CE-6a-(b)):** attribution freezes `TierAtSale` + `TierBonusRate` at sale time; financial
resolution applies `effectiveRate = min(baseResolvedRate + frozenTierBonusRate, MaxEffectiveRate=0.35)` →
`ProviderShareAmount = salePrice × effectiveRate`; `ResolvedRate` keeps the base for audit; forward-only (tier changes
never recompute past rows). Bronze / non-provider → bonus 0 → unchanged.

Table: `sales_attributions`. DB creds: aizen/aizen (from `docker-compose.yaml`). provider2 = **100011**.

---

## Phase 0 — Rebuild + migration
```bash
docker compose build cargodry-api
docker compose up -d --force-recreate cargodry-api
docker compose logs --tail=150 cargodry-api | grep -iE "AddCargoDrySalesAttributionTierSnapshot|applying migration|error|exception|fail" | head
```
Expected: migration `AddCargoDrySalesAttributionTierSnapshot` applied, clean boot, no exceptions.
If the container errors on boot → read the full log, fix, rebuild, repeat.

## Phase 1 — Schema + Bronze-unchanged baseline
```bash
docker compose exec -T postgres psql -U aizen -d aizen -c "\d sales_attributions" | grep -iE "TierAtSale|TierBonusRate"
docker compose exec -T postgres psql -U aizen -d aizen -c "
  SELECT \"Id\",\"ProviderProfileId\",\"TierAtSale\",\"TierBonusRate\",\"CommissionRate\",\"ResolvedRate\",\"ProviderShareAmount\"
    FROM sales_attributions WHERE \"ProviderProfileId\"=100011 ORDER BY \"CreatedAtUtc\" DESC LIMIT 5;"
```
Expected: both columns exist; existing provider2 rows have `TierBonusRate=0` (Bronze / pre-migration), and where
resolved `CommissionRate == ResolvedRate` (no bonus). Historical rows unchanged.

## Phase 2 — Snapshot at sale time (Silver scenario)
Goal: prove a NEW attribution freezes a non-zero tier bonus when the provider's rolling-12-month cumulative crosses a
threshold. Use a disposable **test provider id = 990011** to avoid polluting real data.

1. Backfill cumulative ≥ 5000 (Silver) for 990011 by inserting synthetic **resolved, non-cancelled** attributions
   summing `ProviderShareAmount ≥ 5000` in the last 12 months. **Inspect NOT NULL columns first** (`\d
   sales_attributions`) and insert valid minimal rows (or reuse the module's create/resolve path). Example shape:
   ```sql
   -- adjust columns to the real NOT NULL set; Status must be non-cancelled (e.g. Attributed/SettlementPending)
   INSERT INTO sales_attributions (... "ProviderProfileId","Status","ProviderShareAmount","CreatedAtUtc" ...)
   VALUES (... 990011, <non-cancelled>, 3000, now() ...), (... 990011, <non-cancelled>, 2500, now() ...);
   ```
2. Trigger ONE new attribution for provider 990011 through the activation flow (`CargoDryCommercialActivationService`)
   — activate a kit owned by 990011 (create a minimal kit if needed) so `CreateAttributionAsync` runs.
3. Assert the newest 990011 attribution snapshot:
   ```sql
   SELECT "Id","TierAtSale","TierBonusRate" FROM sales_attributions
    WHERE "ProviderProfileId"=990011 ORDER BY "CreatedAtUtc" DESC LIMIT 1;
   -- expect TierAtSale='SILVER', TierBonusRate=0.02  (from cumulative ≥5000)
   ```

## Phase 3 — Differential payout at resolution (the money proof)
Prove same `salePrice` + same base rate but different frozen bonus ⇒ different `ProviderShareAmount`.

1. Take/create two SettlementPending attributions with identical `salePrice` (e.g. 1000) and a base rate that resolves
   to e.g. 0.20: one with `TierBonusRate=0` (Bronze), one with `TierBonusRate=0.02` (Silver — from Phase 2 or set the
   field directly for the test row).
2. Run **financial resolution** on both through the real path
   (`ResolveCargoDrySalesAttributionFinancialsCommandHandler` — via its endpoint/command; do NOT hand-write the amount).
3. Assert:
   ```sql
   SELECT "Id","TierBonusRate","ResolvedRate","CommissionRate","SalePrice","ProviderShareAmount"
     FROM sales_attributions WHERE "Id" IN (<bronzeId>,<silverId>);
   -- Bronze: CommissionRate = ResolvedRate (0.20), ProviderShareAmount = 200.0000
   -- Silver: CommissionRate = ResolvedRate + 0.02 (0.22), ProviderShareAmount = 220.0000
   ```

## Phase 4 — Cap + Immutability
- **Cap:** set a test attribution with a base rate + bonus exceeding 0.35 (e.g. base 0.34, bonus 0.03) → after
  resolution `CommissionRate = 0.35` (capped), not 0.37.
- **Immutability (forward-only):** on an unresolved 990011 attribution that snapshotted `TierBonusRate=0.02`, lower the
  provider's cumulative below 5000 (delete/cancel the synthetic rows), then resolve → it STILL uses `0.02`
  (frozen), not 0. Already-resolved/settled rows never change.

## Phase 5 — Regression + cleanup
- Provider earnings/tier endpoints (`GET /provider/cargodry/earnings`, `/tier`) still return 200; higher
  `ProviderShareAmount` from bonus rows is reflected in cumulative.
- Remove all disposable test rows (provider 990011 + any test attributions) so real data is clean:
  ```sql
  DELETE FROM sales_attributions WHERE "ProviderProfileId"=990011;
  ```

---

## If any check fails or any error appears
1. Capture the exact exception/mismatch:
   ```bash
   docker compose logs --tail=200 cargodry-api | grep -iE "exception|error|fail|Resolve|EffectiveRate|TierBonus|migration" -A6 | tail -80
   ```
2. Diagnose the root cause against the CE-6a-(b) mechanism (snapshot at creation / EffectiveRate application /
   RecordRuleTrace base vs applied / cap / migration). Common suspects: bonus applied at the wrong place (double-applied
   in monthly settlement resolution), `ResolvedRate` overwritten with the effective rate (audit lost), snapshot not set
   for provider paths, rounding, or migration not applied.
3. Fix the code, `docker compose build cargodry-api && docker compose up -d --force-recreate cargodry-api`, re-run the
   failing phase. Repeat until green.

## Acceptance (all must pass, with pasted output)
- Migration applied; `TierAtSale`/`TierBonusRate` columns present.
- New provider sale snapshots the correct tier (Silver = 0.02) from prior cumulative; Bronze/non-provider = 0.
- Resolution: `effectiveRate = min(base + frozenBonus, 0.35)`; `ProviderShareAmount` reflects it; `ResolvedRate` keeps
  the base (audit). Bronze payout unchanged; Silver payout higher by the bonus; cap enforced.
- Forward-only immutability holds (tier change after snapshot does not alter the frozen bonus / past rows).
- Regression endpoints 200; test data cleaned up. No errors in logs.

## Report
`REPORT_BACKEND.md` ("CE-6a-(b) smoke"): pasted evidence for schema, snapshot, differential payout (Bronze vs Silver),
cap, and immutability; any bug found + fix applied. Confirm real-data untouched (forward-only) and test rows removed.
