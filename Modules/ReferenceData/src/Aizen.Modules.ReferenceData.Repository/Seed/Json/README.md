# ReferenceData JSON Seed Files

This directory contains all JSON seed data for the `Aizen.Modules.ReferenceData` module.

## JSON Format Rules

- All files use **UTF-8** encoding.
- All JSON files use **camelCase** property names.
- Enum values use **integer** representations matching C# enum definitions.
- Boolean fields are lowercase (`true` / `false`).
- Null values are written as `null`.

## Idempotency Rules

All seed operations are idempotent. Running the seed multiple times will not create duplicate records.

| Entity              | Unique Key                                                         |
|---------------------|--------------------------------------------------------------------|
| Currency            | `code`                                                             |
| MeasurementUnit     | `code`                                                             |
| Language            | `code`                                                             |
| TimeZone            | `code`                                                             |
| CountryPhoneCode    | `countryCode`                                                      |
| SystemParameter     | `key`                                                              |
| LookupGroup         | `code`                                                             |
| LookupItem          | `groupCode` + `code`                                               |
| Country document    | `countryCode`                                                      |
| City document       | `countryCode` + `cityCode`                                         |
| District document   | `countryCode` + `cityCode` + `districtCode`                        |
| Neighborhood doc    | `countryCode` + `cityCode` + `districtCode` + `neighborhoodCode`   |
| Street document     | `countryCode` + `cityCode` + `districtCode` + `neighborhoodCode` + `streetCode` |

## Turkish Location Seed Convention

Turkey location data is under `Location/TR/`.

```
Location/TR/
  country.json           — Single country object (not an array)
  cities.json            — Array of city objects
  Districts/
    34-istanbul.json     — Array of districts for city 34
    35-izmir.json        — Array of districts for city 35
  Neighborhoods/
    34-istanbul/
      kadikoy.json       — Array of neighborhoods for Kadikoy
      besiktas.json      — Array of neighborhoods for Besiktas
  Streets/
    34-istanbul/
      kadikoy.json       — Array of streets for Kadikoy
```

The seed service **discovers district, neighborhood, and street files recursively**. To add a new city:
1. Create `Districts/<plate>-<cityslug>.json`
2. Create `Neighborhoods/<plate>-<cityslug>/<districtslug>.json`
3. Create `Streets/<plate>-<cityslug>/<districtslug>.json`

## ⚠️ Production Dataset Warning

The location data in this repository is a **development sample only** (Istanbul/Kadikoy).

The full official Turkey administrative dataset (81 provinces, ~960 districts, ~30,000 neighborhoods, streets) is **not included**.

To import the full official dataset, see `Location/TR/README.md`.

## How to Add a New Country

1. Create `Location/<ISO2>/country.json` (single object)
2. Create `Location/<ISO2>/cities.json` (array)
3. Create `Location/<ISO2>/Districts/` subdirectory with `<code>-<cityslug>.json` files
4. Create `Location/<ISO2>/Neighborhoods/` and `Streets/` subdirectories following the same pattern

## How to Add a New Lookup Group or Item

- To add a group: append an entry to `Lookup/lookup-groups.json`. Set `parentCode` to the parent's `code` or `null` for root.
- To add items: append entries to `Lookup/lookup-items.json`. Set `groupCode` to the target group's `code`.
