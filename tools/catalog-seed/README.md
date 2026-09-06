# Catalog seed generator (boats + engines)

Generates the TR-weighted **vessel** and **engine** brand/model reference dataset
seeded into
`Modules/ReferenceData/.../Seed/Json/Catalog/{vessel-brands,vessel-models,engine-brands,engine-models}.json`
— the files `CatalogJsonSeedService` reads. This is **v1 of a growing catalog**,
not "complete since 1995": expect gaps. The admin review queue + owner "not in
list" submissions + admin CRUD grow it from real usage.

## Regenerate

```bash
cd tools/catalog-seed
python3 build.py     # -> ../../Modules/.../Seed/Json/Catalog/*.json  (overwrites)
                     #  + out/*.review.csv (human-review twin) + a summary report
```

Stdlib only (no third-party deps). No network — the data is compiled facts, not
a live fetch.

## What it produces (last run — v1.1)

| file | rows |
|---|---|
| `vessel-brands.json` | 62 |
| `vessel-models.json` | **1043** |
| `engine-brands.json` | 25 |
| `engine-models.json` | **401** |

v1.1 expands model **lines** into the real selectable **named models** (the
"Beneteau Oceanis" line → `Oceanis 30.1 / 34.1 / 37.1 / 40.1 / 46.1 / 51.1` …,
"Fairline Targa" → `Targa 38 / 40 / 43 / 45 / 50 / 58` …) plus current + archived
back-catalog, and each engine **family** into its **HP variants** (`Yamaha F2.5 →
F450`, `Volvo Penta D4-175 → D4-320`, …). Because most expanded rows have no
confirmed launch year, they ship `needsReview=true` (999/1043 vessel models) for
the admin queue to confirm — intentional and honest for a growing v1.

Boats: Turkish builders (Sirena, Numarine, Vicem, Mengi Yay, Bering, Mazu, Sarp,
Bilgin, Turquoise, Tansu, Su Marine, Alesta) + global majors sold in TR (Beneteau,
Jeanneau, Bavaria, Hanse, Dufour, Lagoon, Fountaine Pajot, Bali, Azimut, Ferretti,
Princess, Sunseeker, Sea Ray, Bayliner, Quicksilver, Boston Whaler, Axopar, Saxdor,
Zodiac/Grand RIBs, Sea-Doo/Yamaha PWC, …). Engines: outboards (Yamaha, Mercury,
Suzuki, Honda, Tohatsu, Parsun, Selva, Evinrude) + electric outboards (Torqeedo,
ePropulsion) + inboard diesels (Volvo Penta, Yanmar, Cummins, Caterpillar, MAN,
MTU, Scania, John Deere, FPT, Doosan, Baudouin, Nanni, Vetus, Steyr, Beta).

## Sourcing methodology (important)

- We **compile facts** — brand, model line, production years, HP, fuel — from the
  **manufacturer's own site**. Every row carries a `source` URL (the official
  page the fact was checked against). We do **NOT** bulk-copy any single protected
  boat/engine database; this is per-row sourcing of individual facts.
- The curated facts live in [`catalog_data.py`](catalog_data.py) as plain tuples
  (`BOAT_BRANDS`, `BOAT_MODELS`, `ENGINE_BRANDS`, `ENGINE_MODELS`). To grow the
  catalog, add rows there and re-run — do not hand-edit the JSON.
- Granularity is the **model line** (production year *range*), not per-year trims.
  Engines are **per HP family** (e.g. `Yamaha F150 → 150 hp → GASOLINE`), not
  every gearcase/shaft variant.

## `needsReview` and `yearFrom`

- `yearFrom` is set **only** where the production-start year is confidently known
  (e.g. Axopar 28 = 2014, Sirena 58 = 2016, Sea-Doo Spark = 2014). Where the start
  year is undocumented in v1 it is **`null`** and the row is auto-flagged
  `needsReview=true` (see `build.py` — a null `yearFrom`/`horsePower` forces the
  flag). Long-running families (most sail/motor lines) are intentionally left
  year-open for the review queue to fill.
- Brands for smaller/custom builders (e.g. Bilgin, Turquoise, Tansu, Sunreef) are
  seeded `needsReview=true` — real but with thin/uncertain model data.

## Type codes — MUST be reconciled before go-live

`vesselTypeCode` (`SAILBOAT`/`MOTORYACHT`/`CATAMARAN`/`RIB`/`PWC`/`SUPERYACHT`),
`fuelTypeCode` (`GASOLINE`/`DIESEL`/`ELECTRIC`) and `engineTypeCode`
(`OUTBOARD`/`INBOARD`/`STERNDRIVE`) are **best-guess coarse codes**, not yet
mapped to the real MARINE lookup vocabulary. Reconcile them to the actual lookup codes (or null them) before these
drive any UI filter. They exist so the wizard's type filter has something to bind.

## How codes are generated

Brand/model `code`s use the **same algorithm** as the C# `CatalogNameNormalizer.Slug`
(upper-invariant → non-alphanumeric runs collapse to `_` → trim `_`, `ITEM`
fallback), with a numeric suffix on collision. So a seeded code is identical to
what an owner/admin submission of the same name would produce — the per-key
idempotent seeder (`GetByCodeAsync`) then upserts cleanly, and a re-run adds only
new rows.

## Verification

`CatalogSeedLoadTests` (in the ReferenceData unit-test project) loads these exact
JSON files through the real `CatalogJsonSeedService` into an in-memory context and
asserts: full counts seed, every model resolves its brand (no orphans), per-row
`source`/`needsReview` reach the entity, `yearFrom=null` rows are flagged, engine
HP carries through, and a second `SeedAsync` is idempotent.
