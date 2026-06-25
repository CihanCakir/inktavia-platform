# AdminPanel BFF Future / Inactive Endpoint Decision Report

## Scope

Classify and handle endpoints for modules that are not active in the current platform release.

## Decision Matrix

| Module | Endpoints | Decision | HTTP Status | Notes |
|--------|-----------|----------|-------------|-------|
| CargoDry | `GET /cargodry/kits`, `POST /cargodry/kits/activate` | 501 Not Implemented | 501 | No active module contract |
| Notification | `GET/POST /notification-templates` | 501 Not Implemented | 501 | No template management module active |
| Payment | `GET /payments/transactions`, `/transactions/kpi`, `/commissions` | 501 Not Implemented | 501 | Payment module is future/inactive |
| Reporting | `GET /reports`, `/reports/kpi`, `/analytics/dashboard` | 501 Not Implemented | 501 | No Reporting module in active list |
| FileStorage list | `GET /files` | 501 Not Implemented | 501 | List-all-files not exposed as admin endpoint yet |

## Implementation

Created `AdminInactiveModulesController.cs` at:

```
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Controllers/V1/AdminInactiveModulesController.cs
```

All endpoints decorated with `[AllowAnonymous]` to prevent redundant auth failures.

Response envelope:

```json
{
  "header": {
    "isSuccess": false,
    "errorCode": 501,
    "message": "Module 'X' is not active in this release."
  },
  "body": null
}
```

## Test Runner Expectation Update Required

Tests in these suites should be updated to expect `501` instead of `200`/`201`:
- `cargoDry.api.json`
- `notification.api.json`
- `payment.api.json`
- `reporting.api.json` (for reports/kpi/analytics/files)

## Follow-ups

- When Payment module activates: remove `GET /payments/*` from this controller and implement real BFF endpoints.
- When Notification template management activates: add real `GET/POST /notification-templates` BFF endpoints.
- When Reporting module activates: add real `GET /reports`, `/reports/kpi`, `/analytics/dashboard` endpoints.
- CargoDry: no planned activation date — keep 501.
