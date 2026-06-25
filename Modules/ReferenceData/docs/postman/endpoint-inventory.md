# ReferenceData Module — Endpoint Inventory

**Module:** ReferenceData  
**Port:** 7104  
**Base URL:** `{{reference_data_api_base_url}}` = `http://localhost:7104/api/v1`  
**Admin Base URL:** `http://localhost:7104/api/v1/admin` (note: admin routes use `/api/v1/admin/reference-data/`)

---

## LookupController (Public)

**Route prefix:** `/api/v1/reference-data/lookup-groups`  
**Tag:** Lookup  
**Auth:** None

| # | Method | Route | Params | Response |
|---|---|---|---|---|
| 1 | GET | /api/v1/reference-data/lookup-groups | onlyActive=true | List<LookupGroupDto> |
| 2 | GET | /api/v1/reference-data/lookup-groups/tree | onlyActive=true | Tree<LookupGroupDto> |
| 3 | GET | /api/v1/reference-data/lookup-groups/{id} | — | LookupGroupDto |
| 4 | GET | /api/v1/reference-data/lookup-groups/lookup-items/{groupCode} | onlyActive=true | List<LookupItemDto> |

**Sample Response (LookupGroupDto):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Vessel Types",
  "code": "VESSEL_TYPES",
  "description": "Types of maritime vessels",
  "parentGroupId": null,
  "sortOrder": 1,
  "isActive": true
}
```

---

## CurrencyController (Public)

**Route prefix:** `/api/v1/reference-data/currencies`  
**Tag:** Currency  
**Auth:** None

| # | Method | Route | Params | Response |
|---|---|---|---|---|
| 1 | GET | /api/v1/reference-data/currencies | onlyActive=true | List<CurrencyDto> |
| 2 | GET | /api/v1/reference-data/currencies/base | — | CurrencyDto (base currency) |
| 3 | GET | /api/v1/reference-data/currencies/{id} | — | CurrencyDto |

**Sample Response (CurrencyDto):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "USD",
  "name": "US Dollar",
  "symbol": "$",
  "decimalPlaces": 2,
  "isBase": false,
  "isActive": true
}
```

---

## LocationController (Public)

**Route prefix:** `/api/v1/reference-data/locations`  
**Tag:** Location  
**Auth:** None

| # | Method | Route | Params | Response |
|---|---|---|---|---|
| 1 | GET | /api/v1/reference-data/locations/countries | onlyActive=true | List<CountryDto> |
| 2 | GET | /api/v1/reference-data/locations/countries/{countryCode} | — | CountryDto |
| 3 | GET | /api/v1/reference-data/locations/{countryCode}/cities | onlyActive=true | List<CityDto> |
| 4 | GET | /api/v1/reference-data/locations/{countryCode}/cities/{cityCode} | — | CityDto |
| 5 | GET | /api/v1/reference-data/locations/{countryCode}/cities/{cityCode}/districts | onlyActive=true | List<DistrictDto> |
| 6 | GET | /api/v1/reference-data/locations/{countryCode}/cities/{cityCode}/districts/{districtCode}/neighborhoods | onlyActive=true | List<NeighborhoodDto> |

**Sample paths:** `/api/v1/reference-data/locations/TR/cities?onlyActive=true`  
**Sample paths:** `/api/v1/reference-data/locations/TR/cities/IST/districts?onlyActive=true`

---

## MeasurementController (Public)

**Route prefix:** `/api/v1/reference-data/measurement-units`  
**Tag:** Measurement  
**Auth:** None

| # | Method | Route | Params | Response |
|---|---|---|---|---|
| 1 | GET | /api/v1/reference-data/measurement-units | onlyActive=true | List<MeasurementUnitDto> |
| 2 | GET | /api/v1/reference-data/measurement-units/by-type/{unitType} | onlyActive=true | List<MeasurementUnitDto> |
| 3 | GET | /api/v1/reference-data/measurement-units/{id} | — | MeasurementUnitDto |

---

## ExchangeRateController (Public)

**Route prefix:** `/api/v1/reference-data/exchange-rates`  
**Tag:** ExchangeRate  
**Auth:** None

| # | Method | Route | Params | Response |
|---|---|---|---|---|
| 1 | GET | /api/v1/reference-data/exchange-rates | fromCurrencyCode, toCurrencyCode | ExchangeRateDto |
| 2 | GET | /api/v1/reference-data/exchange-rates/by-currency/{currencyCode} | — | List<ExchangeRateDto> |
| 3 | GET | /api/v1/reference-data/exchange-rates/history | fromCurrencyCode, toCurrencyCode, startDate, endDate | List<ExchangeRateHistoryDto> |

---

## SystemParameterController (Public)

**Route prefix:** `/api/v1/reference-data/system-parameters`  
**Tag:** SystemParameter  
**Auth:** None

| # | Method | Route | Params | Response |
|---|---|---|---|---|
| 1 | GET | /api/v1/reference-data/system-parameters | onlyActive=true | List<SystemParameterDto> |
| 2 | GET | /api/v1/reference-data/system-parameters/{key} | — | SystemParameterDto |
| 3 | GET | /api/v1/reference-data/system-parameters/by-prefix | prefix, onlyActive=true | List<SystemParameterDto> |

---

## LookupAdminController (Admin)

**Route prefix:** `/api/v1/admin/reference-data/lookup-groups` and `/api/v1/admin/reference-data/lookup-items`  
**Tag:** Admin - Lookup  
**Auth:** Bearer (Admin)

| # | Method | Route | Request Body | Notes |
|---|---|---|---|---|
| 1 | POST | /api/v1/admin/reference-data/lookup-groups | CreateLookupGroupRequest | Creates group |
| 2 | PUT | /api/v1/admin/reference-data/lookup-groups/{id} | UpdateLookupGroupRequest | Updates group |
| 3 | PUT | /api/v1/admin/reference-data/lookup-groups/move | MoveLookupGroupRequest | Moves group in tree |
| 4 | PUT | /api/v1/admin/reference-data/lookup-groups/{id}/activate | — | Activates group |
| 5 | PUT | /api/v1/admin/reference-data/lookup-groups/{id}/deactivate | — | Deactivates group |
| 6 | POST | /api/v1/admin/reference-data/lookup-items | CreateLookupItemRequest | Creates item |
| 7 | PUT | /api/v1/admin/reference-data/lookup-items/{id} | UpdateLookupItemRequest | Updates item |
| 8 | PUT | /api/v1/admin/reference-data/lookup-items/{id}/activate | — | Activates item |
| 9 | PUT | /api/v1/admin/reference-data/lookup-items/{id}/deactivate | — | Deactivates item |

**Sample CreateLookupGroupRequest:**
```json
{
  "name": "Vessel Categories",
  "code": "VESSEL_CATEGORIES",
  "description": "Maritime vessel category types",
  "parentGroupId": null,
  "sortOrder": 10,
  "isActive": true
}
```

**Sample CreateLookupItemRequest:**
```json
{
  "groupId": "{{lookupGroupId}}",
  "name": "Cargo Ship",
  "code": "CARGO_SHIP",
  "description": "General cargo vessels",
  "iconKey": "cargo-ship",
  "colorCode": "#4A90E2",
  "sortOrder": 1,
  "isDefault": false,
  "isActive": true
}
```

---

## CurrencyAdminController (Admin)

**Route prefix:** `/api/v1/admin/reference-data/currencies`  
**Tag:** Admin - Currency  
**Auth:** Bearer (Admin)

| # | Method | Route | Request Body | Notes |
|---|---|---|---|---|
| 1 | POST | /api/v1/admin/reference-data/currencies | CreateCurrencyRequest | Creates currency |
| 2 | PUT | /api/v1/admin/reference-data/currencies/{id} | UpdateCurrencyRequest | Updates currency |
| 3 | PUT | /api/v1/admin/reference-data/currencies/{id}/set-base | — | Sets as base currency |
| 4 | PUT | /api/v1/admin/reference-data/currencies/{id}/activate | — | Activates currency |
| 5 | PUT | /api/v1/admin/reference-data/currencies/{id}/deactivate | — | Deactivates currency |

**Sample CreateCurrencyRequest:**
```json
{
  "code": "EUR",
  "name": "Euro",
  "symbol": "€",
  "decimalPlaces": 2,
  "isActive": true
}
```

---

## LocationAdminController (Admin)

**Route prefix:** `/api/v1/admin/reference-data/locations`  
**Tag:** Admin - Location  
**Auth:** Bearer (Admin)

| # | Method | Route | Request Body | Notes |
|---|---|---|---|---|
| 1 | POST | /api/v1/admin/reference-data/locations/countries | CreateCountryRequest | Creates country |
| 2 | PUT | /api/v1/admin/reference-data/locations/countries/{countryCode} | UpdateCountryRequest | Updates country |
| 3 | POST | /api/v1/admin/reference-data/locations/cities | CreateCityRequest | Creates city |
| 4 | PUT | /api/v1/admin/reference-data/locations/{countryCode}/cities/{cityCode} | UpdateCityRequest | Updates city |
| 5 | POST | /api/v1/admin/reference-data/locations/districts | CreateDistrictRequest | Creates district |
| 6 | PUT | /api/v1/admin/reference-data/locations/{countryCode}/cities/{cityCode}/districts/{districtCode} | UpdateDistrictRequest | Updates district |
| 7 | POST | /api/v1/admin/reference-data/locations/neighborhoods | CreateNeighborhoodRequest | Creates neighborhood |
| 8 | PUT | /api/v1/admin/reference-data/locations/{countryCode}/cities/{cityCode}/districts/{districtCode}/neighborhoods/{neighborhoodCode} | UpdateNeighborhoodRequest | Updates neighborhood |
| 9 | POST | /api/v1/admin/reference-data/locations/streets | CreateStreetRequest | Creates street |

**Sample CreateCountryRequest:**
```json
{
  "code": "TR",
  "name": "Turkey",
  "defaultCurrencyCode": "TRY",
  "phoneCode": "+90",
  "isActive": true
}
```
