# 09 — Update Test Runner, Postman, and Expectations

Update local BFF test runner and Postman artifacts so they match the final model:

React-like BFF requests:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Do not send browser `Authorization` header.

For future/inactive modules:

- tests must not fail expecting 200 unless the endpoint is intentionally enabled,
- document expected 404/501/feature-disabled behavior,
- keep optional local-demo mocks clearly guarded.

Generate:

```text
docs/reports/admin-panel-bff-test-runner-postman-alignment-report.md
```
