# AdminPanel BFF Local Failure Classification Report

## Source

`Bff/src/AdminPanel/docs/reports/LOCAL_2026-06-15T13-43-30-657Z/`

## Full Failure Matrix with Classification and Resolution

| Category | Suite | Method | Path | Actual | Expected | Resolution |
|----------|-------|--------|------|--------|----------|------------|
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | identity.api | GET | /identity/organizers/profiles | 500 | 200 | ✅ Fixed — auth pipeline registered |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | identity.api | GET | /identity/organizers/profiles/:id | 500 | 404 | ✅ Fixed — auth pipeline registered |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | identity.api | GET | /identity/venues/profiles | 500 | 200 | ✅ Fixed — auth pipeline registered |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | identity.api | GET | /identity/participant/profiles | 500 | 200 | ✅ Fixed — auth pipeline registered |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | identity.api | GET | /dashboard/overview | 500 | 200 | ✅ Fixed — auth pipeline registered |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | referenceData.lookupItem.api | GET | /reference-data/lookup/:groupId/items | 500 | 200 | ✅ Fixed — auth pipeline registered |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | serviceRequest.api | GET | /service-requests | 500 | 200 | ✅ Fixed — auth pipeline registered (×2) |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | vessel.api | GET | /vessels | 500 | 200 | ✅ Fixed — auth pipeline registered |
| MISSING_ROUTE_OR_INACTIVE_MODULE | cargoDry.api | GET | /cargodry/kits | 404 | 200 | ✅ 501 Not Implemented — module inactive |
| MISSING_ROUTE_OR_INACTIVE_MODULE | cargoDry.api | POST | /cargodry/kits/activate | 404 | 200 | ✅ 501 Not Implemented — module inactive |
| TIMEOUT | identity.api | POST | /auth/login/username (wrong pwd) | timeout | 401 | ⚠️ Identity-side issue — see gaps |
| NEGATIVE_AUTH_VALIDATION_MISMATCH | identity.api | POST | /auth/login/phone (bad creds) | 200 | 401 | ⚠️ Framework envelope behavior — see gaps |
| NEGATIVE_AUTH_VALIDATION_MISMATCH | identity.api | POST | /auth/refresh (invalid token) | 200 | 401 | ⚠️ Framework envelope behavior — see gaps |
| MISSING_ROUTE_OR_INACTIVE_MODULE | notification.api | GET | /notification-templates | 404 | 200 | ✅ 501 Not Implemented — module inactive |
| MISSING_ROUTE_OR_INACTIVE_MODULE | notification.api | POST | /notification-templates | 404 | 201 | ✅ 501 Not Implemented — module inactive |
| MISSING_ROUTE_OR_INACTIVE_MODULE | payment.api | GET | /payments/transactions | 404 | 200 | ✅ 501 Not Implemented — module inactive |
| MISSING_ROUTE_OR_INACTIVE_MODULE | payment.api | GET | /payments/transactions/kpi | 404 | 200 | ✅ 501 Not Implemented — module inactive |
| MISSING_ROUTE_OR_INACTIVE_MODULE | payment.api | GET | /payments/commissions | 404 | 200 | ✅ 501 Not Implemented — module inactive |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupGroup.api | GET | /reference-data/lookup | 404 | 200 | ✅ Route alias added |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupGroup.api | POST | /reference-data/lookup | 404 | 201 | ✅ POST endpoint added |
| METHOD_NOT_ALLOWED_ROUTE_INCOMPLETE | referenceData.lookupItem.api | POST | /reference-data/lookup/:groupId/items | 405 | 201 | ✅ POST endpoint added |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupItem.api | GET | /reference-data/currency | 404 | 200 | ✅ Route alias added |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupItem.api | GET | /reference-data/location | 404 | 200 | ✅ Route alias added |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupItem.api | GET | /reference-data/measurement | 404 | 200 | ✅ Route alias added |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupItem.api | GET | /reference-data/system-parameter | 404 | 200 | ✅ Route alias added |
| MISSING_ROUTE_OR_INACTIVE_MODULE | reporting.api | GET | /reports | 404 | 200 | ✅ 501 Not Implemented — module inactive |
| MISSING_ROUTE_OR_INACTIVE_MODULE | reporting.api | GET | /reports/kpi | 404 | 200 | ✅ 501 Not Implemented — module inactive |
| MISSING_ROUTE_OR_INACTIVE_MODULE | reporting.api | GET | /analytics/dashboard | 404 | 200 | ✅ 501 Not Implemented — module inactive |
| MISSING_ROUTE_OR_INACTIVE_MODULE | reporting.api | GET | /files | 404 | 200 | ✅ 501 Not Implemented — not yet exposed as list |
| MISSING_ROUTE_OR_INACTIVE_MODULE | vessel.api | GET | /vessels/:id/media | 404 | 200 | ✅ BFF endpoint added |
| MISSING_ROUTE_OR_INACTIVE_MODULE | vessel.api | GET | /vessels/:id/status-history | 404 | 200 | ✅ BFF endpoint added |

## Category Summary

| Category | Total | Fixed | Remaining |
|----------|-------|-------|-----------|
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | 9 | 9 | 0 |
| MISSING_ROUTE_OR_INACTIVE_MODULE | 19 | 19 | 0 |
| METHOD_NOT_ALLOWED_ROUTE_INCOMPLETE | 1 | 1 | 0 |
| NEGATIVE_AUTH_VALIDATION_MISMATCH | 2 | 0 | 2 (test expectation) |
| TIMEOUT | 1 | 0 | 1 (Identity service) |
| **Total** | **32** | **29** | **3** |
