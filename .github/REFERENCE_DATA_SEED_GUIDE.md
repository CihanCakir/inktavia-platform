# ReferenceData JSON Seed Guide

## Required seed data categories

### EF Core PostgreSQL entities

```text
CurrencyEntity
ExchangeRateEntity
ExchangeRateHistoryEntity
MeasurementUnitEntity
LookupGroupEntity
LookupItemEntity
SystemParameterEntity
LanguageEntity
TimeZoneEntity
CountryPhoneCodeEntity
```

### MongoDB documents

```text
LocationCountryDocument
LocationCityDocument
LocationDistrictDocument
LocationNeighborhoodDocument
LocationStreetDocument
```

## Seed JSON folder convention

```text
Aizen.Modules.ReferenceData.Repository/Seed/Json/
  Currency/
    currencies.json
  ExchangeRate/
    exchange-rates.json
    exchange-rate-histories.json
  Measurement/
    measurement-units.json
  Lookup/
    lookup-groups.json
    lookup-items.json
    lookup-tree-marine.json
  System/
    system-parameters.json
    languages.json
    time-zones.json
    country-phone-codes.json
  Location/
    TR/
      country.json
      cities.json
      Districts/
        01-adana.json
        34-istanbul.json
      Neighborhoods/
        34-istanbul/
          kadikoy.json
      Streets/
        34-istanbul/
          kadikoy.json
```

## Turkey data rule

The agent must create the JSON structure and importer. If the repository already contains official Turkey location files, use them. If not, create schema-compatible JSON template files and clearly mark the full official Turkey dataset as a required import task. Do not invent incomplete fake administrative data as production seed.
