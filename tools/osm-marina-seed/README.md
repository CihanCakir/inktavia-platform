# OSM marina + fishing-harbour seed generator

Generates the Turkey marina / fishing-harbour reference dataset seeded into
`Modules/ReferenceData/.../Seed/Json/Marina/marinas.json` (the file the
`MarinaJsonSeedService` reads). **Data © OpenStreetMap contributors, ODbL** —
keep the attribution (`ATTRIBUTION.txt` ships next to the seed).

## Regenerate

```bash
cd tools/osm-marina-seed
python3 1_fetch.py     # Overpass -> raw/{marinas,fishing,provinces}.json  (raw/ is gitignored)
python3 2_build.py     # -> ../../Modules/.../Seed/Json/Marina/{marinas.json,ATTRIBUTION.txt} + out/marinas.review.csv
```

No third-party Python deps (stdlib only: urllib, ray-casting PIP). If Overpass
rate-limits, `1_fetch.py` falls back across mirrors (kumi.systems, mail.ru).

## What `2_build.py` does

1. **Extract** marinas (`leisure=marina`) + fishing harbours (union of
   `harbour=fishing`, `harbour:category=fishing`, `seamark:...=fishing`,
   `name~Balıkçı Barınağı`). Ways/relations use their `center`.
2. **Province** via local point-in-polygon against admin_level=4 boundaries
   (rings stitched from `out geom`); coastal points just offshore fall back to
   the nearest province (flagged only when >2 km out). Province name → il plate
   `cityCode` via the Location seed (`cities.json`, 13 provinces seeded there).
3. **Classify** MARINA vs FISHING_HARBOR — a name containing "Balıkçı Barınağı"
   wins over a stray `leisure=marina` tag.
4. **Clean / dedupe** — same/similar name within 300 m, or any two points
   <80 m, collapse to one (MARINA > FISHING; real name > blank; more tags win).
   Obvious strays (oil/LNG terminals, shipyards, naval) dropped. Unnamed →
   `Marina (<place>)` / `Balıkçı Barınağı (<place>)` + `needsReview`.
5. **Reconcile** the interim 22 hand marinas (`interim_marinas.json`, a frozen
   snapshot — kept separate from the output so re-runs are idempotent): OSM wins
   coords/name but **keeps the interim code** so FK references survive. Pass 1 =
   300 m or exact name; pass 2 = a distinctive name token (len≥5, not a province
   name) within 20 km, since the hand coords were approximate. Unmatched interim
   rows are kept (FK-safe) and flagged.
6. **Emit** the seed JSON (array; `latitude`/`longitude` keys), `ATTRIBUTION.txt`,
   and `out/marinas.review.csv` (human review twin). Prints counts.

## Output schema (per row)

`code` (unique slug), `name`, `type` (MARINA|FISHING_HARBOR), `countryCode`,
`cityCode` (il plate or null), `province`, `district` (nullable), `latitude`,
`longitude`, `osmId`, `needsReview`, `isActive`.
