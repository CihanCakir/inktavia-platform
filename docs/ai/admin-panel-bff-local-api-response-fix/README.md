# AdminPanel BFF Local API Response Fix Copilot Package

This package instructs GitHub Copilot Agent to repair the AdminPanel BFF after a local frontend/API test run.

It uses the JSON/HTML reports generated under:

```text
Bff/src/AdminPanel/docs/reports/LOCAL_2026-06-15T13-43-30-657Z/
```

The task is to:

- inspect all failed frontend-to-BFF API calls,
- compare each request/response against the real AdminPanel BFF endpoint catalog and controllers,
- fix missing active endpoints,
- fix BFF authentication scheme errors,
- debug failing module calls with the current Identity + Keycloak service-token model,
- align Postman/test runner expectations where an endpoint is intentionally inactive/future,
- generate final reports.

## Final security model

Browser/Admin Web to AdminPanel BFF:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

AdminPanel BFF to internal modules:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

The browser must not provide Keycloak tokens.

## How to run

Place this package under:

```text
docs/ai/admin-panel-bff-local-api-response-fix/
```

Then run Copilot Agent with:

```text
Read and execute `docs/ai/admin-panel-bff-local-api-response-fix/RUN_THIS_FIRST_SINGLE_PROMPT.md`; use `manifest.json`, every file under `reference/`, and every step prompt under `ai/admin-panel-bff-local-api-response-fix/` as mandatory references; inspect the local BFF report directory `Bff/src/AdminPanel/docs/reports/LOCAL_2026-06-15T13-43-30-657Z/`, fix all active AdminPanel BFF endpoint/auth/response failures according to the final Identity + BFF-side Keycloak service-token model, update or feature-gate inactive/future endpoints, validate with build/test/local API rerun, and generate final reports under `docs/reports/`.
```
