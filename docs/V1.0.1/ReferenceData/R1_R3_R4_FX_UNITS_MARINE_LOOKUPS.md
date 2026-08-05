# R1 + R3 + R4 — ReferenceData: point-in-time FX resolve · pricing units · marine pricing lookups

> **Repo:** `addesso-project` — **ReferenceData module** (+ a remote call for ServiceRequest). First of the V1.0.1
> second-wave roadmap; feeds SR S2 (attributes) / S3 (price-book + FX) / S4 (travel). **The infra already exists** —
> ExchangeRate (+history+service), MeasurementUnit (+CRUD), LookupGroup/Item (tree + JSON seed + `LookupGroupType.Marine`).
> These are **additive gaps** (a resolve query + two seed sets). **R2 (category VAT) is excluded — YMM-gated.** Follow the
> project's cacheable-query + cache-invalidation conventions (e.g. `GetExchangeRatesByCurrency` is `IAizenQueryHandlerCacheable`).

## R1 — point-in-time exchange-rate resolve (offer-time FX snapshot)
**Gap:** there's `GetExchangeRateHistory` (list over a range) + `GetExchangeRatesByCurrency`, but **no single
resolve-at-date**. SR S3 needs "the effective rate for EUR/USD→TL **at offer-creation time**" to snapshot (no
re-valuation after acceptance — §20.7).
- Add `ResolveExchangeRateQuery(fromCurrencyCode, toCurrencyCode, DateTimeOffset asOfUtc)` → the **effective rate at that
  instant** (latest history row with `EffectiveFrom <= asOf`; clear "no rate" result if none). Cacheable (keyed by
  from/to/asOf-day) + cache invalidation on rate upsert.
- Expose it: a controller endpoint + an **internal typed remote call** the ServiceRequest module can call at offer
  creation (mirror the existing internal remote-call pattern; envelope-correct). UTC-safe (timestamptz rule — asOf is
  `Kind=Utc`).
- Do **not** change the existing history/by-currency queries or the ExchangeRate entity.

## R3 — pricing measurement units (PerLiter/PerKm/PerSquareMeter/PerHour/PerDay)
**Gap:** `MeasurementUnitEntity` + CRUD exist, but the specific units the `PricingMethod` needs aren't guaranteed seeded.
- **Seed (idempotent, dedupe by code)** the units: **litre (L), kilometre (km), square-metre (m²), hour (saat), day
  (gün)** — with code, symbol, display name (tr/en if the model carries locale), and `BaseUnitCode` where sensible.
  Align the codes with the SR `PricingMethod` values (PerLiter/PerKm/PerSquareMeter/PerHour/PerDay) so S1's method maps to
  a unit.
- Confirm they're queryable via the existing measurement query/service (no new API needed unless the lookup-by-code is
  missing — add a `GetMeasurementUnitByCode` if absent). Reuse the CRUD; don't rebuild.

## R4 — marine pricing lookups (engine / paint / work difficulty)
**Gap:** the LookupGroup tree + JSON seed (`lookup-groups.json` / `LookupJsonSeedService`) + `LookupGroupType.Marine`
exist, but the **pricing attribute lookups** don't. SR S2 (`PricingAttributeDefinition`) will reference these.
- **Add to the marine lookup JSON seed** (idempotent) the groups + items: **EngineInstallationType** (İçten takma /
  Kıçtan takma / Şaft…), **EngineType/Class** (e.g. V8/V12/V16, inboard/outboard classes), **PaintType** (antifouling /
  epoxy / topcoat…), **WorkDifficulty** (Kolay/Orta/Zor or 1–5). Use stable codes + tr/en labels; place under the Marine
  group tree (respect the parent-child + circular-reference guards already in `ILookupTreeService`).
- Confirm queryable via the existing lookup query/tree service (group-by-code + items). No new API unless a
  by-group-code lookup is missing.
- (Coordinate the codes with SR S2 so `PricingAttributeDefinition` can reference them — S2 is the consumer, later.)

## Don't-break / QA
- Additive only: one new resolve query/endpoint/remote-call (R1) + two idempotent seed sets (R3, R4). Existing
  ReferenceData entities/CRUD/seed/cache untouched. Duplicate-seed protection (dedupe by code). Cacheable + invalidation
  per convention. UTC-safe. Migrations only if a column is genuinely needed (prefer seed/query over schema). Builds
  clean.

## Verify
1. **R1:** `ResolveExchangeRate("EUR","TRY", <asOf>)` returns the effective rate at that instant (and a clear empty
   result before the earliest rate); the internal remote call is reachable from ServiceRequest; cache hit on repeat.
2. **R3:** the 5 pricing units exist and resolve by code; re-running the seed doesn't duplicate.
3. **R4:** the marine pricing lookup groups + items exist under the Marine tree and resolve by group code; re-seed
   idempotent; tree guards intact.

## Report
`docs/V1.0.1/ReferenceData/REPORT_R1_R3_R4.md`: the resolve query + remote call (R1), the seeded units (R3), the seeded
marine lookups (R4), idempotency proof, and the codes S2/S3/S4 will reference. Next wave: SR S2 (pricing attributes,
references R4) / S3 (price-book + FX, references R1) / S4 (travel — I2 already done).
