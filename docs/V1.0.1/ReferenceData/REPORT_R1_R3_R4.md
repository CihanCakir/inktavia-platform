# REPORT — R1 · R3 · R4 (ReferenceData: point-in-time FX resolve · pricing units · marine pricing lookups)

> Scope: additive only. One new resolve query/endpoint/remote-call (R1) + two idempotent seed sets (R3, R4).
> Existing ReferenceData entities/CRUD/cache untouched. R2 (category VAT) excluded — YMM-gated.
> Source spec: `docs/V1.0.1/ReferenceData/R1_R3_R4_FX_UNITS_MARINE_LOOKUPS.md`.

---

## R1 — point-in-time exchange-rate resolve (offer-time FX snapshot)

**What SR S3 gets:** "the effective rate for FROM/TO at offer-creation time" — the latest history row with
`RateDate <= asOf` — to snapshot once at offer creation (no re-valuation after acceptance, §20.7).

### New query (cacheable)
`ResolveExchangeRateQuery(fromCurrencyCode, toCurrencyCode, DateTimeOffset asOfUtc)` →
`ExchangeRateResolveDto` (always non-null; `HasRate=false` ⇒ a **clear empty result** when no rate is effective
at/before the instant).

- Query: `…/Application/ExchangeRate/Queries/ResolveExchangeRate/ResolveExchangeRateQuery.cs`
- Handler: `…/ResolveExchangeRateQueryHandler.cs` — `IAizenQueryHandlerCacheable`, `Distributed`, 15-min TTL
  (mirrors `GetExchangeRateQueryHandler`).
- Service: `IExchangeRateReferenceService.ResolveRateAsync(...)` +
  `ExchangeRateReferenceService.ResolveRateAsync(...)`.
- Repo: `IExchangeRateRepository.GetRateAsOfAsync(from, to, DateTime asOfUtc)` +
  `ExchangeRateRepository` impl — `ExchangeRateHistories.AsNoTracking().Where(from && to && RateDate <= asOf)
  .OrderByDescending(RateDate).FirstOrDefault()` (uses the existing
  `(FromCurrencyCode, ToCurrencyCode, RateDate)` index).
- Mapping: `ExchangeRateMappingExtensions.ToResolveDto(this ExchangeRateHistoryEntity?, from, to, asOfUtc)`
  (Repository mappings — the set the service actually uses).

### UTC-safe (timestamptz rule)
The service forces `asOf = asOfUtc.UtcDateTime` (Kind=Utc) before the comparison, matching the global UTC
read/write convention (`UtcDateTimeConverter` on read; `NormalizeDateTimeProperties` on write). No entity or
existing history/by-currency query was changed.

### Cache key = from / to / **as-of-day** (per spec)
The decorator auto-builds the key from the query's **public** properties. The query exposes
`FromCurrencyCode`, `ToCurrencyCode`, and `AsOfDay` (a `"yyyy-MM-dd"` string) — so the key granularity is
**from/to/as-of-day**. The exact instant lives in an **`internal DateTimeOffset AsOfUtc`** (excluded from the
key because reflection sees only public props) and is what the handler resolves against. Net effect: correct
instant-level resolution on a cache **miss**, day-level key sharing on hits, 15-min TTL bounding staleness.

### Invalidation on rate upsert
`ReferenceDataCacheInvalidationService.InvalidateExchangeRateAsync(from, to)` (already called by
`UpdateExchangeRateCommandHandler`) now also removes the resolve key for the **current UTC day**:
`ResolveExchangeRateQueryHandler:FromCurrencyCode_{from}|ToCurrencyCode_{to}|AsOfDay_{today}|`.
Rationale: a rate upsert most affects "as of now" resolutions; past-day keys are immutable in the append-only
history, and the 15-min TTL bounds any residual staleness (consistent with the module's existing
"wildcard removal not supported" limitation).

### Internal typed remote call (ServiceRequest → ReferenceData)
Added to the existing `IServiceRequestReferenceDataRemoteCall` (ServiceRequest `.Abstraction/RemoteCall/`):
```csharp
[AizenRemoteCallGet("/api/v1/reference-data/exchange-rates/resolve?fromCurrencyCode={fromCurrencyCode}&toCurrencyCode={toCurrencyCode}&asOfUtc={asOfUtc}")]
Task<AizenApiResponse<SrExchangeRateResolveDto>> ResolveExchangeRate(string fromCurrencyCode, string toCurrencyCode, DateTimeOffset asOfUtc);
```
+ local minimal `SrExchangeRateResolveDto { HasRate, From/ToCurrencyCode, Rate, RateDate, AsOfUtc }`.
Envelope-correct (`AizenApiResponse<T>`); auto-registered as a Refit client by `AddAizenRemoteCall`.
**Reachability:** `docker-compose.yaml` already wires
`RemoteCalls__IServiceRequestReferenceDataRemoteCall__BaseUrl: http://reference-data-api:8080` for the
`service-request-api` container; a `localhost:7104` fallback was added to ServiceRequest `appsettings.json`
for host runs.

### Controller endpoint
`GET /api/v1/reference-data/exchange-rates/resolve?fromCurrencyCode=&toCurrencyCode=&asOfUtc=` on the existing
read-only `ExchangeRateController`.

**Codes S3 references:** `GET /api/v1/reference-data/exchange-rates/resolve` (public) /
`IServiceRequestReferenceDataRemoteCall.ResolveExchangeRate(...)` (internal). Result contract:
`HasRate`, `Rate`, `RateDate`, `AsOfUtc`.

---

## R3 — pricing measurement units

**Gap:** `LITER`(L), `HOUR`(h), `DAY`(d) and `METER` already existed; `KILOMETER` and `SQUARE_METER` did not.

### Seeded (idempotent, dedupe by code) — `Seed/Json/Measurement/measurement-units.json`
Two additive rows (existing rows untouched):

| Code | Name | Symbol | UnitType | Factor→base | BaseUnitCode | Aligns to PricingMethod |
|------|------|--------|----------|-------------|--------------|--------------------------|
| `KILOMETER` | Kilometer | km | Length (1) | 1000 | `METER` | `PerKm` (5) |
| `SQUARE_METER` | Square Meter | m² | Area (8) | 1 | — | `PerSquareMeter` (7) |

Full pricing-unit ↔ `PricingMethod` map (codes S1 uses to bind a method to a unit):

| Code | PricingMethod |
|------|----------------|
| `LITER` | `PerLiter` (6) |
| `KILOMETER` | `PerKm` (5) |
| `SQUARE_METER` | `PerSquareMeter` (7) |
| `HOUR` | `PerHour` (3) |
| `DAY` | `PerDay` (4) |

> Note: the `MeasurementUnitEntity` carries a single `Name` (no locale field), so no tr/en split is possible;
> symbols are metric (`km`, `m²`) and language-neutral.

### Query by code (was missing at the Application layer)
Added `GetMeasurementUnitByCodeQuery(code)` + handler (cacheable, 24h) → `IMeasurementReferenceService.
GetByCodeAsync` → repo `GetByCodeAsync` (already existed). Endpoint:
`GET /api/v1/reference-data/measurement-units/by-code/{code}`. `CreateMeasurementUnit` now invalidates the
by-code key for the created code (update/activate/deactivate stay id-based — code is immutable — with the 24h
TTL as the by-code safety net, matching the existing by-type precedent).

---

## R4 — marine pricing lookups

**Gap:** the marine lookup tree + JSON seed + `LookupGroupType.Marine` existed, but the pricing-attribute
lookups did not. Added under a new `MARINE_PRICING` container (child of the existing `MARINE` root) so S2's
`PricingAttributeDefinition` has a clean namespace and there is **no collision** with the pre-existing
vessel-scoped `ENGINE_TYPE` group.

### Groups added — `Seed/Json/Lookup/lookup-groups.json` (parentCode `MARINE`/`MARINE_PRICING`, `groupType` 2)
`MARINE_PRICING` → `ENGINE_INSTALLATION_TYPE`, `ENGINE_CLASS`, `PAINT_TYPE`, `WORK_DIFFICULTY`.

### Items added — `Seed/Json/Lookup/lookup-items.json` (stable codes; tr in `name`, en in `description`)

| Group | Item codes (tr / en) |
|-------|----------------------|
| `ENGINE_INSTALLATION_TYPE` | `INBOARD` İçten Takma/Inboard · `OUTBOARD` Kıçtan Takma/Outboard · `STERNDRIVE` Kıç Tahrik/Sterndrive · `SHAFT` Şaft Tahrik/Shaft · `POD` Pod Tahrik/Pod |
| `ENGINE_CLASS` | `V6` · `V8` · `V12` · `V16` · `INLINE` Sıralı/Inline |
| `PAINT_TYPE` | `ANTIFOULING` Zehirli Boya/Antifouling · `EPOXY` Epoksi/Epoxy · `TOPCOAT` Son Kat/Topcoat · `PRIMER` Astar/Primer · `GELCOAT` Jelkot/Gelcoat |
| `WORK_DIFFICULTY` | `EASY` Kolay/Easy · `MEDIUM` Orta/Medium · `HARD` Zor/Hard (green/amber/red colorCodes) |

> **Schema-limitation decision:** `LookupGroupEntity`/`LookupItemEntity` carry a single `Name` + optional
> `Description` — there is no tr/en locale field. Both labels are preserved by convention: **`name` = Turkish
> (the UI label), `description` = English.** Flagged for S2 in case a dedicated localization field is wanted
> later. `Code` (stable, uppercase) remains the language-neutral key S2 references.

### Query by group code (already existed)
Items resolve via the existing `GetLookupItemsByGroupQuery` →
`GET /api/v1/reference-data/lookup-groups/lookup-items/{groupCode}` (e.g. `…/lookup-items/PAINT_TYPE`) and
`LookupReferenceService.GetItemsByGroupCodeAsync`. Tree guards intact: the new groups only ever attach to an
existing/earlier parent, so the seed path's topological-sort + parent-resolution guards (and the
`ILookupTreeService` move-time circular-reference guard) are untouched.

**Codes S2 references:** group codes `ENGINE_INSTALLATION_TYPE`, `ENGINE_CLASS`, `PAINT_TYPE`,
`WORK_DIFFICULTY` (+ the `MARINE_PRICING` container) and the item codes above.

---

## Cross-cutting change — seed idempotency guard (necessary for additive seeds to apply)

The three JSON seeders (`MeasurementJsonSeedService`, `LookupJsonSeedService.SeedGroupsAsync` /
`SeedItemsAsync`) each began with a **whole-table `AnyAsync` early-return**. Their inner loops are already
per-code upserts (each class doc literally states *"Idempotency key: Code"*), but the whole-table guard meant
that on an **already-seeded** DB the loop never ran — so appended rows would **never** be inserted. This was
observed live: the pre-existing DB had the R3 units (their table happened to be empty at last seed) but was
**missing** the R4 marine groups (LookupGroups was already populated → old guard blocked them).

**Change:** removed the whole-table early-return in all three seeders, keeping the per-code upsert loop as
authored. Result: additive rows apply on populated DBs, and re-runs are still dedupe-by-code (no duplicates).
This is the single deviation from "seed untouched" and is required for the feature to function; it aligns each
seeder with its own documented idempotency contract. (Trade-off: seed data is re-asserted on every startup —
acceptable for reference data; new rows only ever attach to existing parents so tree integrity is preserved.)

---

## Verification

Verified live against the local stack (`reference-data-api` on :7104, Postgres `inktavia_store`) after rebuilding
the image with these changes and re-seeding on boot.

- **Build:** `Aizen.Modules.ReferenceData` and `Aizen.Modules.ServiceRequest.Abstraction` build clean (0 errors).

- **The guard fix (observed live):** before the change the DB had the R3 units but was **missing** all R4 marine
  groups — `LookupGroups` was already populated, so the old whole-table guard blocked the additive JSON. After
  removing the guard and restarting, the seeder ran on the populated table (`INSERT INTO ref."LookupItems" …`
  in the logs) and the 5 groups + 18 items appeared.

- **R1 — point-in-time resolve (DB-level, the exact repo query `RateDate <= asOf ORDER BY RateDate DESC LIMIT 1`,
  with two seeded EUR/TRY history rows @ 2026-07-01=35.50 and 2026-08-01=37.10):**
  - as-of `2026-06-15` (before earliest) → **no row** ⇒ `HasRate=false` (clear empty result) ✓
  - as-of `2026-07-15` → **35.50** (07-01 row) ✓
  - as-of `2026-08-05` → **37.10** (08-01 row) ✓
  - Endpoint is a thin passthrough over this query, mirroring the proven `GetRate` action; caching is the
    framework `IAizenQueryHandlerCacheable` decorator (same as the existing FX queries) → cache-hit-on-repeat is
    structural. Internal remote call reachability is wired in `docker-compose.yaml`
    (`service-request-api` → `http://reference-data-api:8080`). _(Admin write endpoints were not exercised — no
    legitimate Admin token; history rows were seeded directly in the dev DB and removed after.)_

- **R3 — units resolve by code (HTTP 200):**
  - `GET …/measurement-units/by-code/KILOMETER` → `{code:KILOMETER, symbol:km, unitType:Length, factor:1000, baseUnitCode:METER}` ✓
  - `GET …/measurement-units/by-code/SQUARE_METER` → `{code:SQUARE_METER, symbol:m², unitType:Area, factor:1}` ✓

- **R4 — lookups resolve by group code (HTTP 200) + tree intact:**
  - `GET …/lookup-groups/lookup-items/WORK_DIFFICULTY` → EASY/MEDIUM/HARD (Kolay/Orta/Zor) with
    `groupHierarchyPath = MARINE/MARINE_PRICING/WORK_DIFFICULTY`, `groupType = Marine`, colorCodes ✓
  - DB: 5 groups at correct levels (`MARINE`→`MARINE_PRICING`→leaf, Level 1/2 materialized paths), 18 items
    (5/5/5/3), `name`=tr + `description`=en ✓

- **Idempotency (dedupe-by-code):** the seeder now runs on every startup. After a second forced restart the
  counts were **unchanged** (44 groups / 18 marine items / 17 units) and there were **zero** duplicate codes
  (`GROUP BY code HAVING count(*)>1` returned empty for both units and items). ✓

### Next wave
SR **S2** (pricing attributes → references the R4 group/item codes) · **S3** (price-book + FX → references the
R1 resolve query/remote-call) · **S4** (travel — I2 already done).
