# 05 - Complete ReferenceData Endpoint Mapping

ReferenceData endpoint coverage is incomplete. Re-scan and add missing ReferenceData endpoints to AdminPanel BFF.

## Must audit areas

- Currency
- ExchangeRate
- ExchangeRateHistory if present
- MeasurementUnit
- LookupGroup
- LookupItem
- Lookup tree
- Lookup group by id/code/path/parent if present
- Lookup items by group/id/code if present
- SystemParameter if present
- Language if present
- TimeZone if present
- CountryPhoneCode if present
- Country
- City
- District
- Neighborhood
- Street
- Marine lookup groups/items

## Controller naming

Use names such as:

```text
ReferenceDataController
CurrenciesController
ExchangeRatesController
MeasurementUnitsController
LookupsController
LocationsController
SystemParametersController
LanguagesController
TimeZonesController
```

Do not prefix every controller with `Admin`.

## Application structure

Use feature-based query/command folders with separate handler files.

## Remote call

Use ReferenceDataRemoteService via AizenRemoteCall.

## Documentation

Generate ReferenceData coverage table in:

```text
Bff/src/AdminPanel/docs/postman/reference-data-endpoint-coverage.md
```
