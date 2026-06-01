# 07 — Create Seed Services

Create seed services under:

```text
Aizen.Modules.ReferenceData.Repository/Seed/Services/
```

Every class/interface must include DocumentationInfo.

## Interface

Create according to the existing project convention. Recommended:

```text
Aizen.Modules.ReferenceData.Domain/Interface/Service/IReferenceDataJsonSeedService.cs
```

Methods:

```csharp
Task SeedAllAsync(CancellationToken cancellationToken = default);
Task SeedEfEntitiesAsync(CancellationToken cancellationToken = default);
Task SeedLocationDocumentsAsync(CancellationToken cancellationToken = default);
Task SeedLookupTreeAsync(CancellationToken cancellationToken = default);
```

## Services

Create:

```text
ReferenceDataJsonSeedService
CurrencyJsonSeedService
ExchangeRateJsonSeedService
MeasurementJsonSeedService
LookupJsonSeedService
SystemJsonSeedService
LocationJsonSeedService
```

## Execution order

```text
1. Currency
2. Measurement
3. System definitions
4. Lookup tree/groups
5. Lookup items
6. Exchange rates
7. Location country
8. Location cities
9. Location districts
10. Location neighborhoods
11. Location streets
```

## Idempotency rules

Use these unique keys:

```text
Currency: Code
ExchangeRate: FromCurrencyCode + ToCurrencyCode
ExchangeRateHistory: FromCurrencyCode + ToCurrencyCode + RateDate
MeasurementUnit: Code
LookupGroup: Code
LookupItem: LookupGroupId + Code
SystemParameter: Key
Language: Code
TimeZone: Code
CountryPhoneCode: CountryCode
Country document: CountryCode
City document: CountryCode + CityCode
District document: CountryCode + CityCode + DistrictCode
Neighborhood document: CountryCode + CityCode + DistrictCode + NeighborhoodCode
Street document: CountryCode + CityCode + DistrictCode + NeighborhoodCode + StreetCode
```

## Save changes

Use the existing UnitOfWork/MiniUow/DbContext save standard.

## Output

Report created service classes and methods.
