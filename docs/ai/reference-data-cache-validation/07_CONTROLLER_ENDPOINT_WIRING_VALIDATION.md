# 07 — Controller Endpoint Wiring Validation

Validate ReferenceData API endpoints are connected to the correct commands and queries.

## Rules

Controllers must not contain business logic. Controllers must use Abstraction request models, dispatch commands/queries, return the existing API response wrapper, and must not return EF entities or Mongo documents.

## Endpoint checks

### Currency

```text
GET /api/v1/reference-data/currencies -> GetCurrencyListQuery
GET /api/v1/reference-data/currencies/{code} -> GetCurrencyDetailQuery
POST /api/v1/reference-data/currencies -> CreateCurrencyCommand
PUT /api/v1/reference-data/currencies/{code} -> UpdateCurrencyCommand
PATCH /api/v1/reference-data/currencies/{code}/activate -> ActivateCurrencyCommand
PATCH /api/v1/reference-data/currencies/{code}/deactivate -> DeactivateCurrencyCommand
PATCH /api/v1/reference-data/currencies/{code}/set-base -> SetBaseCurrencyCommand
```

### ExchangeRate

```text
GET /api/v1/reference-data/exchange-rates/current -> GetExchangeRateQuery
GET /api/v1/reference-data/exchange-rates/history -> GetExchangeRateHistoryQuery
POST /api/v1/reference-data/exchange-rates/update -> UpdateExchangeRateCommand
POST /api/v1/reference-data/exchange-rates/sync -> SyncExchangeRatesCommand
```

### Lookup

```text
GET /api/v1/reference-data/lookups/groups -> GetLookupGroupListQuery
GET /api/v1/reference-data/lookups/groups/tree -> GetLookupGroupTreeQuery
GET /api/v1/reference-data/lookups/groups/{groupCode} -> GetLookupGroupDetailQuery
GET /api/v1/reference-data/lookups/groups/{groupCode}/children -> GetLookupGroupChildrenQuery
GET /api/v1/reference-data/lookups/groups/{groupCode}/breadcrumb -> GetLookupGroupBreadcrumbQuery
POST /api/v1/reference-data/lookups/groups -> CreateLookupGroupCommand
PUT /api/v1/reference-data/lookups/groups/{groupCode} -> UpdateLookupGroupCommand
PATCH /api/v1/reference-data/lookups/groups/{groupCode}/move -> MoveLookupGroupCommand
GET /api/v1/reference-data/lookups/groups/{groupCode}/items -> GetLookupItemsByGroupQuery
POST /api/v1/reference-data/lookups/items -> CreateLookupItemCommand
PUT /api/v1/reference-data/lookups/items/{itemCode} -> UpdateLookupItemCommand
```

### Measurement

```text
GET /api/v1/reference-data/measurements/units -> GetMeasurementUnitListQuery
GET /api/v1/reference-data/measurements/units/{code} -> GetMeasurementUnitDetailQuery
GET /api/v1/reference-data/measurements/units/by-type/{unitType} -> GetMeasurementUnitsByTypeQuery
POST /api/v1/reference-data/measurements/units -> CreateMeasurementUnitCommand
PUT /api/v1/reference-data/measurements/units/{code} -> UpdateMeasurementUnitCommand
```

### Location

```text
GET /api/v1/reference-data/locations/countries -> GetCountryListQuery
GET /api/v1/reference-data/locations/countries/{countryCode} -> GetCountryDetailQuery
GET /api/v1/reference-data/locations/countries/{countryCode}/cities -> GetCityListByCountryQuery
GET /api/v1/reference-data/locations/cities/{cityCode}/districts -> GetDistrictListByCityQuery
GET /api/v1/reference-data/locations/districts/{districtCode}/neighborhoods -> GetNeighborhoodListByDistrictQuery
POST /api/v1/reference-data/locations/search -> SearchLocationQuery
POST /api/v1/reference-data/locations/validate -> ValidateLocationQuery
```

### SystemParameter

```text
GET /api/v1/reference-data/system-parameters -> GetSystemParameterListQuery
GET /api/v1/reference-data/system-parameters/{key} -> GetSystemParameterByKeyQuery
GET /api/v1/reference-data/system-parameters/by-prefix -> GetSystemParametersByPrefixQuery
POST /api/v1/reference-data/system-parameters -> CreateSystemParameterCommand
PUT /api/v1/reference-data/system-parameters/{key} -> UpdateSystemParameterCommand
```

## Output

Report endpoint matrix, missing endpoints, wrong mappings fixed, DTO verification and remaining manual API tests.
