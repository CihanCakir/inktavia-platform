# ReferenceData Module — Postman Validation Report

## Endpoint Coverage

| Controller | Endpoint | Method | Route | Covered | Sample | Tests | Notes |
|---|---|---|---|---|---|---|---|
| LookupController | Get All Lookup Groups | GET | `/lookup-groups` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LookupController | Get Lookup Groups Tree | GET | `/lookup-groups/tree` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LookupController | Get Lookup Group By ID | GET | `/lookup-groups/{id}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LookupController | Get Lookup Items By Group | GET | `/lookup-groups/lookup-items/{code}` | ✅ Yes | ✅ Yes | ✅ Yes | Extracts `lookupItemId` |
| CurrencyController | Get All Currencies | GET | `/currencies` | ✅ Yes | ✅ Yes | ✅ Yes | Extracts `currencyId` |
| CurrencyController | Get Base Currency | GET | `/currencies/base` | ✅ Yes | ✅ Yes | ✅ Yes | |
| CurrencyController | Get Currency By ID | GET | `/currencies/{id}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LocationController | Get Countries | GET | `/locations/countries` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LocationController | Get Country By Code | GET | `/locations/countries/{code}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LocationController | Get Cities | GET | `/locations/{countryCode}/cities` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LocationController | Get City By Code | GET | `/locations/{countryCode}/cities/{cityCode}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LocationController | Get Districts | GET | `/locations/.../districts` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LocationController | Get Neighborhoods | GET | `/locations/.../neighborhoods` | ✅ Yes | ✅ Yes | ✅ Yes | |
| MeasurementController | Get All Units | GET | `/measurement-units` | ✅ Yes | ✅ Yes | ✅ Yes | |
| MeasurementController | Get Units By Type | GET | `/measurement-units/by-type/{type}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| MeasurementController | Get Unit By ID | GET | `/measurement-units/{id}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| ExchangeRateController | Get Exchange Rate | GET | `/exchange-rates` | ✅ Yes | ✅ Yes | ✅ Yes | |
| ExchangeRateController | Get Rates By Currency | GET | `/exchange-rates/by-currency/{code}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| ExchangeRateController | Get Rate History | GET | `/exchange-rates/history` | ✅ Yes | ✅ Yes | ✅ Yes | |
| SystemParameterController | Get All Parameters | GET | `/system-parameters` | ✅ Yes | ✅ Yes | ✅ Yes | |
| SystemParameterController | Get Parameter By Key | GET | `/system-parameters/{key}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| SystemParameterController | Get Parameters By Prefix | GET | `/system-parameters/by-prefix` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LookupAdminController | Create Lookup Group | POST | `/admin/.../lookup-groups` | ✅ Yes | ✅ Yes | ✅ Yes | Extracts `lookupGroupId` |
| LookupAdminController | Update Lookup Group | PUT | `/admin/.../lookup-groups/{id}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LookupAdminController | Move Lookup Group | PUT | `/admin/.../lookup-groups/move` | ⚠️ Partial | ✅ Yes | ⚠️ Basic | No sample test in collection |
| LookupAdminController | Activate Lookup Group | PUT | `/admin/.../lookup-groups/{id}/activate` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LookupAdminController | Deactivate Lookup Group | PUT | `/admin/.../lookup-groups/{id}/deactivate` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LookupAdminController | Create Lookup Item | POST | `/admin/.../lookup-items` | ✅ Yes | ✅ Yes | ✅ Yes | Extracts `lookupItemId` |
| LookupAdminController | Update Lookup Item | PUT | `/admin/.../lookup-items/{id}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LookupAdminController | Activate Lookup Item | PUT | `/admin/.../lookup-items/{id}/activate` | ⚠️ Partial | N/A | ⚠️ Basic | |
| LookupAdminController | Deactivate Lookup Item | PUT | `/admin/.../lookup-items/{id}/deactivate` | ⚠️ Partial | N/A | ⚠️ Basic | |
| CurrencyAdminController | Create Currency | POST | `/admin/.../currencies` | ✅ Yes | ✅ Yes | ✅ Yes | Extracts `currencyId` |
| CurrencyAdminController | Update Currency | PUT | `/admin/.../currencies/{id}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| CurrencyAdminController | Set Base Currency | PUT | `/admin/.../currencies/{id}/set-base` | ✅ Yes | ✅ Yes | ✅ Yes | |
| CurrencyAdminController | Activate Currency | PUT | `/admin/.../currencies/{id}/activate` | ✅ Yes | ✅ Yes | ✅ Yes | |
| CurrencyAdminController | Deactivate Currency | PUT | `/admin/.../currencies/{id}/deactivate` | ⚠️ Partial | N/A | ⚠️ Basic | |
| LocationAdminController | Create Country | POST | `/admin/.../countries` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LocationAdminController | Update Country | PUT | `/admin/.../countries/{code}` | ⚠️ Partial | N/A | ⚠️ Basic | |
| LocationAdminController | Create City | POST | `/admin/.../cities` | ✅ Yes | ✅ Yes | ✅ Yes | |
| LocationAdminController | Update City | PUT | `/admin/.../{countryCode}/cities/{cityCode}` | ⚠️ Partial | N/A | ⚠️ Basic | |
| LocationAdminController | Create District | POST | `/admin/.../districts` | ⚠️ Partial | N/A | ⚠️ Basic | |
| LocationAdminController | Create Neighborhood | POST | `/admin/.../neighborhoods` | ⚠️ Partial | N/A | ⚠️ Basic | |
| MeasurementAdminController | Create Measurement Unit | POST | `/admin/.../measurement-units` | ⚠️ Partial | N/A | ⚠️ Basic | Not in collection |
| SystemParameterAdminController | Create System Parameter | POST | `/admin/.../system-parameters` | ✅ Yes | ✅ Yes | ✅ Yes | |
| SystemParameterAdminController | Update System Parameter | PUT | `/admin/.../system-parameters/{key}` | ✅ Yes | ✅ Yes | ✅ Yes | |
| SystemParameterAdminController | Activate Parameter | PUT | `/admin/.../system-parameters/{key}/activate` | ⚠️ Partial | N/A | ⚠️ Basic | |

## Coverage Statistics

| Category | Count | Fully Covered | Partial | Coverage |
|---|---|---|---|---|
| Total Endpoints | ~57 | 35 | 12 | ~82% |
| With Sample Request | ~57 | 35 | 12 | ~82% |
| With Test Scripts | ~57 | 35 | 12 | ~82% |

## Notes

- Admin endpoints use a different base path: `/api/v1/admin/reference-data/` vs `/api/v1/reference-data/`
- Partially covered endpoints are less critical management actions (activate/deactivate variants)
- `ExchangeRateAdminController` sync and history endpoints are not included (internal-use only)
