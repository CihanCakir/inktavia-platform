# Country seed generator (full ISO-3166-1)

Generates `Modules/ReferenceData/.../Seed/Json/Location/countries.json` — the flat
array `LocationJsonSeedService.SeedAllCountriesAsync` upserts into MongoDB
(`reference_location_countries`). Powers the mobile flag picker's full searchable
list + pinned yacht-flag states (US, MT, MH, IT, GR, GB, FR, NL, PA, KY).

## Regenerate

```bash
cd tools/countries-seed
python3 build.py     # -> ../../Modules/.../Seed/Json/Location/countries.json  (206 countries)
```

Stdlib only. Edit the `COUNTRIES` table in `build.py` and re-run to change the set.

## Row shape (`LocationCountrySeedModel`)

```json
{ "countryCode": "GR", "numericCode": "300",
  "name": { "tr": "Yunanistan", "en": "Greece" },
  "defaultCurrencyCode": "EUR", "phoneCode": "+30", "isActive": true }
```

`name` always carries `en` (the `LocationNameResolver` fallback) + `tr` (the
Turkish picker label). Data is the public ISO-3166-1 alpha-2 + numeric standard,
ISO-4217 default currency, and E.164 dialing codes.

## Seeding notes

- The seed reads a single **array** file (`countries.json`) via `ReadListAsync`,
  distinct from the per-`<ISO2>/country.json` folders that carry the city/district
  tree (only `TR/` is populated). The country upsert is idempotent on `countryCode`,
  so the array seed and the `TR/` folder overlay coexist.
- **Requires `ReferenceDataSeed.SeedLocationDocuments = true`** (and `Enabled = true`)
  on the environment that runs the seed — it is `false` in committed appsettings, so
  dev must enable it for the Mongo location seed (countries + TR tree) to run.
