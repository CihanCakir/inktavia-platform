# Local BFF Test Report Summary

Source report directory expected inside the backend repository:

```text
Bff/src/AdminPanel/docs/reports/LOCAL_2026-06-15T13-43-30-657Z/
```

Uploaded report snapshot:

```text
Total tests: 52
Passed: 20
Failed: 32
Pass rate: 38%
```

## Suite Summary

| Suite file | Total | Passed | Failed |
| --- | --- | --- | --- |
| cargoDry.api.json | 2 | 0 | 2 |
| identity.api.json | 10 | 2 | 8 |
| notification.api.json | 3 | 1 | 2 |
| payment.api.json | 4 | 1 | 3 |
| referenceData.lookupGroup.api.json | 4 | 2 | 2 |
| referenceData.lookupItem.api.json | 8 | 2 | 6 |
| reporting.api.json | 5 | 1 | 4 |
| serviceRequest.api.json | 8 | 6 | 2 |
| vessel.api.json | 8 | 5 | 3 |

## Failure Categories

| Category | Count |
| --- | --- |
| MISSING_ROUTE_OR_INACTIVE_MODULE | 19 |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | 9 |
| NEGATIVE_AUTH_VALIDATION_MISMATCH | 2 |
| TIMEOUT | 1 |
| METHOD_NOT_ALLOWED_ROUTE_INCOMPLETE | 1 |

## Failure Matrix

| Category | Suite | Method | Endpoint | Actual | Expected | Test name |
| --- | --- | --- | --- | --- | --- | --- |
| MISSING_ROUTE_OR_INACTIVE_MODULE | cargoDry.api.json | GET | /cargodry/kits | 404 | 200 | GET /cargodry/kits — should return kit list or 404 |
| MISSING_ROUTE_OR_INACTIVE_MODULE | cargoDry.api.json | POST | /cargodry/kits/activate | 404 | 200 | POST /cargodry/kits/activate — should attempt QR activation |
| TIMEOUT | identity.api.json | POST | /auth/login/username | None | 401 | POST /auth/login/username — should return 400/401 with wrong password |
| NEGATIVE_AUTH_VALIDATION_MISMATCH | identity.api.json | POST | /auth/login/phone | 200 | 401 | POST /auth/login/phone — should return 400/401 with bad phone credentials |
| NEGATIVE_AUTH_VALIDATION_MISMATCH | identity.api.json | POST | /auth/refresh | 200 | 401 | POST /auth/refresh — should return 400/401 for invalid refresh token |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | identity.api.json | GET | /identity/organizers/profiles | 500 | 200 | GET /identity/organizers/profiles — should return paginated organizer profiles |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | identity.api.json | GET | /identity/organizers/profiles/00000000-0000-0000-0000-000000000001 | 500 | 404 | GET /identity/organizers/profiles/:id — should return 404 for unknown organizer |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | identity.api.json | GET | /identity/venues/profiles | 500 | 200 | GET /identity/venues/profiles — should return paginated venue profiles |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | identity.api.json | GET | /identity/participant/profiles | 500 | 200 | GET /identity/participant/profiles — should return paginated participant profiles |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | identity.api.json | GET | /dashboard/overview | 500 | 200 | GET /dashboard/overview — should return dashboard overview |
| MISSING_ROUTE_OR_INACTIVE_MODULE | notification.api.json | GET | /notification-templates | 404 | 200 | GET /notification-templates — should return list or 404 |
| MISSING_ROUTE_OR_INACTIVE_MODULE | notification.api.json | POST | /notification-templates | 404 | 201 | POST /notification-templates — should attempt to create a template |
| MISSING_ROUTE_OR_INACTIVE_MODULE | payment.api.json | GET | /payments/transactions | 404 | 200 | GET /payments/transactions — should return list or 404 if module inactive |
| MISSING_ROUTE_OR_INACTIVE_MODULE | payment.api.json | GET | /payments/transactions/kpi | 404 | 200 | GET /payments/transactions/kpi — should return KPIs or 404 |
| MISSING_ROUTE_OR_INACTIVE_MODULE | payment.api.json | GET | /payments/commissions | 404 | 200 | GET /payments/commissions — should return list or 404 |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupGroup.api.json | GET | /reference-data/lookup | 404 | 200 | GET /reference-data/lookup — should return paginated lookup groups |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupGroup.api.json | POST | /reference-data/lookup | 404 | 201 | POST /reference-data/lookup — should create a new lookup group |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | referenceData.lookupItem.api.json | GET | /reference-data/lookup/00000000-0000-0000-0000-000000000001/items | 500 | 200 | GET /reference-data/lookup/:groupId/items — should return item list |
| METHOD_NOT_ALLOWED_ROUTE_INCOMPLETE | referenceData.lookupItem.api.json | POST | /reference-data/lookup/00000000-0000-0000-0000-000000000001/items | 405 | 201 | POST /reference-data/lookup/:groupId/items — should attempt to create an item |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupItem.api.json | GET | /reference-data/currency | 404 | 200 | GET /reference-data/currency — should return currency list |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupItem.api.json | GET | /reference-data/location | 404 | 200 | GET /reference-data/location — should return location list |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupItem.api.json | GET | /reference-data/measurement | 404 | 200 | GET /reference-data/measurement — should return measurement list |
| MISSING_ROUTE_OR_INACTIVE_MODULE | referenceData.lookupItem.api.json | GET | /reference-data/system-parameter | 404 | 200 | GET /reference-data/system-parameter — should return system parameters |
| MISSING_ROUTE_OR_INACTIVE_MODULE | reporting.api.json | GET | /reports | 404 | 200 | GET /reports — should return list or 404 |
| MISSING_ROUTE_OR_INACTIVE_MODULE | reporting.api.json | GET | /reports/kpi | 404 | 200 | GET /reports/kpi — should return KPIs or 404 |
| MISSING_ROUTE_OR_INACTIVE_MODULE | reporting.api.json | GET | /analytics/dashboard | 404 | 200 | GET /analytics/dashboard — should return analytics data or 404 |
| MISSING_ROUTE_OR_INACTIVE_MODULE | reporting.api.json | GET | /files | 404 | 200 | GET /files — should return file list |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | serviceRequest.api.json | GET | /service-requests | 500 | 200 | GET /service-requests — should return paginated list |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | serviceRequest.api.json | GET | /service-requests | 500 | 200 | GET /service-requests — filter by status PENDING |
| AUTH_PIPELINE_DEFAULT_SCHEME_500 | vessel.api.json | GET | /vessels | 500 | 200 | GET /vessels — should return paginated vessel list |
| MISSING_ROUTE_OR_INACTIVE_MODULE | vessel.api.json | GET | /vessels/00000000-0000-0000-0000-000000000001/media | 404 | 200 | GET /vessels/:id/media — should return media list or 404 |
| MISSING_ROUTE_OR_INACTIVE_MODULE | vessel.api.json | GET | /vessels/00000000-0000-0000-0000-000000000001/status-history | 404 | 200 | GET /vessels/:id/status-history — should return status history or 404 |

## Important observations

1. Several failures are not endpoint implementation problems but an inbound BFF authentication pipeline problem:
   `No authenticationScheme was specified, and there was no DefaultChallengeScheme found.`
2. Current test requests still send an `Authorization` header with an Identity token. Under the final Admin Web model, React-like BFF calls must send only `X-Aizen-User-Token`.
3. Multiple endpoints are missing or feature/inactive-module related. Do not blindly implement inactive module business logic. Classify each endpoint against real BFF catalog, active module controllers, and Admin Web route requirements.
4. ReferenceData has route mismatches: `/reference-data/lookup` and `/reference-data/lookup/:groupId/items` must be aligned against the actual BFF catalog and active controllers.
5. Negative authentication cases return success or timeout; login/refresh error handling must be fixed or test expectations corrected only if the current Identity contract intentionally uses envelope errors with HTTP 200.
