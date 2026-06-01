# 06 — Mongo, Seed, Lookup, ExchangeRate and Location Validation

Validate core runtime behavior.

## Mongo index initializer

Verify index service exists, creates required indexes idempotently, does not crash if indexes already exist, is registered in DI and is called from startup/starter operation if required.

Required indexes:

```text
reference_location_countries: countryCode unique
reference_location_cities: countryCode + cityCode unique
reference_location_districts: countryCode + cityCode + districtCode unique
reference_location_neighborhoods: countryCode + cityCode + districtCode + neighborhoodCode unique
reference_location_streets: countryCode + cityCode + districtCode + neighborhoodCode + streetCode index
```

## Seed idempotency

Verify seed service does not duplicate Currency, MeasurementUnit, LookupGroup, LookupItem, Language, TimeZone or CountryPhoneCode. Seed should use code uniqueness.

## LookupGroup tree validation

Verify root groups have null parent, level 0 and `HierarchyPath = Code`. Verify child level/path rules, tree query, children query and breadcrumb query.

## MoveLookupGroup validation

Verify a group cannot move under itself or its own descendant, move to root works, move under another parent works, descendant levels/paths refresh and lookup tree cache invalidates.

## ExchangeRate update validation

Verify update creates current rate if missing, updates if existing, creates history on each update, stores raw provider payload if provided and invalidates exchange-rate cache.

## Location DTO validation

Verify location queries return DTOs, localized name fallback works and location validation checks parent-child consistency.

## Output

Report Mongo index, seed, lookup tree, MoveLookupGroup, exchange-rate history, location DTO validation and issues fixed.
