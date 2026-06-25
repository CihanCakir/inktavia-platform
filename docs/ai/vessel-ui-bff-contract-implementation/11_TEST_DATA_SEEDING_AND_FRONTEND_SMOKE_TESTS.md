# 11 — Test Data and Frontend Smoke Tests

Use existing mock/demo seeding if available. If no data exists, document that endpoints can only be shape-tested.

Run BFF smoke tests:

```bash
GET /api/v1/admin-panel/vessels?index=0&size=5
GET /api/v1/admin-panel/vessels/{id}
GET /api/v1/admin-panel/vessels/{id}/documents
GET /api/v1/admin-panel/vessels/{id}/media
```

Use valid AdminPanel BFF auth headers.

Check:

- `header.isSuccess`
- body wrapper shape
- pagination shape
- no Refit deserialize exception
- no auth 401/403 with admin token
- null-safe empty arrays for CargoDry/ServiceRequest when unavailable

Write `docs/reports/vessel-ui-bff-smoke-test-report.md`.
