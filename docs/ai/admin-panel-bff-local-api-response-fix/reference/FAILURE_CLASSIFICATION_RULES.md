# Failure Classification Rules

Classify every failed local test before changing code.

## Category A — AUTH_PIPELINE_DEFAULT_SCHEME_500

Symptom:

```text
No authenticationScheme was specified, and there was no DefaultChallengeScheme found.
```

Action:

- Fix AdminPanel BFF authentication registration.
- Add a default Identity-header authentication scheme or repository-standard Aizen BFF auth scheme.
- Add `AdminPanelAccess` policy.
- Do not solve by accepting browser Keycloak tokens.

## Category B — MISSING_ROUTE_OR_INACTIVE_MODULE

Symptom:

```text
404 with empty body
```

Action:

- Check if endpoint exists in `docs/admin-web-client/admin-panel-bff-endpoint-catalog.md`.
- Check actual BFF controllers.
- Check active module controllers.
- If active and required, implement BFF endpoint.
- If future/inactive, do not invent business logic; feature-gate or align tests.

## Category C — METHOD_NOT_ALLOWED_ROUTE_INCOMPLETE

Symptom:

```text
405 Allow: GET
```

Action:

- Route exists for GET only.
- Add missing POST/PUT/PATCH/DELETE BFF endpoint only if active module contract exists.
- Otherwise update endpoint/test to match read-only behavior.

## Category D — NEGATIVE_AUTH_VALIDATION_MISMATCH

Symptom:

```text
Invalid credentials or invalid refresh token returns 200 success.
```

Action:

- Debug Identity/BFF auth facade.
- Ensure failed login/refresh returns 400/401 or a documented Aizen error envelope.
- Do not let invalid auth return `header.isSuccess = true`.

## Category E — TIMEOUT

Symptom:

```text
ECONNABORTED / timeout
```

Action:

- Debug remote call timeout, async deadlock, Identity error path, or missing cancellation.
- Failed credential checks must fail fast.
