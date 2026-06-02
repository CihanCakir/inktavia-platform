# Copilot Instructions — ReferenceData JSON Seed

You are working in the Inktavia Marine OS repository.

## Module boundary

ReferenceData manages static/shared reference data only:

```text
Country, City, District, Neighborhood, Street
Currency, ExchangeRate, ExchangeRateHistory
MeasurementUnit
LookupGroup, LookupItem
SystemParameter
Language, TimeZone, CountryPhoneCode
```

Do not add nearby users, map markers, radius query, geo-index, map bounds or live location tracking.

## Architecture rules

- Do not create `ReferenceData.Infrastructure`.
- All seed files and seed services must be under `Aizen.Modules.ReferenceData.Repository`.
- EF entities are stored in PostgreSQL through `ReferenceDataDbContext`.
- Location documents are stored in MongoDB through `ILocationRepository` / Mongo repository.
- Mongo location documents inherit from `AizenDocumentBase`.
- LookupGroup is a self-referencing tree.
- Every public class and interface must include `DocumentationInfo` using the existing project standard.
- If `DocumentationInfo` does not exist, create or reuse the project-approved attribute as previously defined.

## JSON seed rules

- Every seedable entity/document must have a JSON file.
- Every JSON file must have a seed model class.
- Every seed model class must map to an entity/document.
- Seed services must be idempotent.
- Seed services must use code-based uniqueness checks.
- Turkey location data must be organized as separate JSON files to avoid huge single-file memory usage.
