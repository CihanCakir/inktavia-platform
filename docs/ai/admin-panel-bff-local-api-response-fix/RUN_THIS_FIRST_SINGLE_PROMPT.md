# RUN THIS FIRST — AdminPanel BFF Local API Response Fix

Read this file first, then execute the full package.

## Mandatory package files

Use these files as mandatory references:

```text
docs/ai/admin-panel-bff-local-api-response-fix/manifest.json
docs/ai/admin-panel-bff-local-api-response-fix/reference/*
docs/ai/admin-panel-bff-local-api-response-fix/ai/admin-panel-bff-local-api-response-fix/*
```

## Source local report directory

Inspect the local API test report directory:

```text
Bff/src/AdminPanel/docs/reports/LOCAL_2026-06-15T13-43-30-657Z/
```

Expected files:

```text
00_SUMMARY.json
00_SUMMARY.html
cargoDry.api.json
identity.api.json
notification.api.json
payment.api.json
referenceData.lookupGroup.api.json
referenceData.lookupItem.api.json
reporting.api.json
serviceRequest.api.json
vessel.api.json
```

If the directory path differs, search for the same run id:

```text
LOCAL_2026-06-15T13-43-30-657Z
```

## Main objective

Repair AdminPanel BFF so the React Admin Web can reliably call all active BFF endpoints and receive correct responses.

This includes:

1. Read every JSON report.
2. Build a failure matrix.
3. Classify every failure.
4. Fix active endpoint failures.
5. Add missing active BFF endpoints where the real AdminPanel BFF contract requires them.
6. Do not invent inactive module business logic.
7. For future/inactive endpoints, either feature-gate, return explicit controlled responses, or align tests/frontend expectations.
8. Fix `No authenticationScheme was specified` failures.
9. Fix negative auth cases that return success or hang.
10. Debug internal module calls using the final Keycloak service-token model.
11. Re-run validation and generate reports.

## Final token/security model

React/Admin Web to AdminPanel BFF:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

AdminPanel BFF to internal module APIs:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

The BFF must not require or forward browser-provided Keycloak tokens.

## Execute step prompts in order

```text
00_MASTER_PROMPT.md
01_IMPORT_AND_CLASSIFY_LOCAL_TEST_FAILURES.md
02_FIX_BFF_INBOUND_AUTH_SCHEME_AND_ADMIN_POLICY.md
03_AUDIT_ENDPOINT_CATALOG_CONTROLLER_AND_FRONTEND_CONTRACT.md
04_FIX_ACTIVE_MODULE_ENDPOINTS_AND_REMOTE_CALLS.md
05_HANDLE_MISSING_FUTURE_OR_INACTIVE_ENDPOINTS.md
06_FIX_AUTH_NEGATIVE_CASES_AND_REFRESH_FLOW.md
07_FIX_REFERENCE_DATA_ROUTE_METHOD_AND_RESPONSE_GAPS.md
08_FIX_DASHBOARD_VESSEL_SERVICE_REQUEST_RESPONSE_ERRORS.md
09_UPDATE_TEST_RUNNER_POSTMAN_AND_REPORT_EXPECTATIONS.md
10_VALIDATION_FINAL_REPORTS_AND_RERUN.md
