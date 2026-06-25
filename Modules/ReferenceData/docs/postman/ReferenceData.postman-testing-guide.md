# ReferenceData Module — Postman Testing Guide

## Overview

ReferenceData module provides reference data lookup endpoints (public) and admin management endpoints (admin-only). Port: **7104**.

## Prerequisites

### Environment Variables Required

| Variable | Value |
|---|---|
| `reference_data_api_base_url` | `http://localhost:7104/api/v1/reference-data` |
| `reference_data_api_root_url` | `http://localhost:7104` |
| `admin_panel_client_id` | `admin-panel` |
| `admin_username` | `test_admin_user` |
| `default_password` | `Password123!` |
| `keycloak_token_url` | `{{keycloak_base_url}}/realms/{{keycloak_realm}}/protocol/openid-connect/token` |

### Services Required

- Keycloak (port 8080)
- ReferenceData API (port 7104)
- PostgreSQL / configured database
- Seeded reference data (countries, currencies, etc.)

## Auth Setup

### Public Endpoints (01–06 folders)

No authentication required. Run requests directly.

### Admin Endpoints (07–10 folders)

1. Run `00 - Auth Setup > Get Admin Token`
2. Verify `active_access_token` is set in environment
3. Proceed with admin requests

## Test Sequence

### Public Data Reads

1. `01 - Lookup > Get All Lookup Groups` → stores `lookupGroupId` if available
2. `02 - Currency > Get All Currencies` → stores `currencyId`
3. `03 - Location > Get Countries` → verify TR exists
4. `04 - Measurement > Get All Measurement Units`
5. `05 - Exchange Rate > Get Exchange Rate` (USD → EUR)
6. `06 - System Parameters > Get All System Parameters`

### Admin Management

1. Run `00 - Auth Setup > Get Admin Token`
2. `07 - Admin - Lookup > Create Lookup Group` → stores `lookupGroupId`
3. `07 - Admin - Lookup > Create Lookup Item` → stores `lookupItemId`
4. `08 - Admin - Currency > Create Currency` → stores `currencyId`
5. `09 - Admin - Location > Create Country`
6. `10 - Admin - System Parameters > Create System Parameter`

## Expected Responses

### Get All Lookup Groups

```json
HTTP 200 OK
[
  {
    "id": "3fa85f64-...",
    "name": "Vessel Types",
    "code": "VESSEL_TYPES",
    "isActive": true
  }
]
```

### Create Lookup Group

```json
HTTP 200 OK or 201 Created
{
  "id": "3fa85f64-...",
  "name": "Test Vessel Categories",
  "code": "TEST_VESSEL_CATS"
}
```

## Common Errors

| Error | Cause | Fix |
|---|---|---|
| 401 Unauthorized | Missing admin token for admin endpoints | Run `Get Admin Token` first |
| 403 Forbidden | Non-admin user | Ensure admin role in Keycloak |
| 404 Not Found | ID/code doesn't exist | Check variable values in environment |
| 409 Conflict | Code already exists | Use a unique code value |

## Notes

- Country/City codes are case-sensitive (`TR`, `IST`, etc.)
- Lookup group codes should follow `UPPER_SNAKE_CASE` convention
- Exchange rates may not be seeded in dev — check database before testing `05 - Exchange Rate`
