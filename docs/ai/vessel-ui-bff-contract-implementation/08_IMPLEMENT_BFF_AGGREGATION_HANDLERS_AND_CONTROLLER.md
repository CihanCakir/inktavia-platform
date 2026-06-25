# 08 — Implement BFF Aggregation Handlers and Controller

Use existing AdminPanel BFF CQRS/controller pattern.

Implement MVP endpoints:

```text
GET /api/v1/admin-panel/vessels
GET /api/v1/admin-panel/vessels/{id}
GET /api/v1/admin-panel/vessels/{id}/documents
GET /api/v1/admin-panel/vessels/{id}/media
```

Implement or feature-gate Post-MVP endpoints:

```text
POST /api/v1/admin-panel/vessels/{id}/documents/{documentId}/approve
POST /api/v1/admin-panel/vessels/{id}/documents/{documentId}/replace
POST /api/v1/admin-panel/vessels
GET /api/v1/admin-panel/vessels/export
```

Auth:

- Use existing `AdminPanelAccess` policy for protected BFF endpoints.
- Keep service token forwarding to internal modules.

Mapping:

- Compute daysUntilExpiry/documentStatus/isCurrent in BFF when needed.
- Return empty optional arrays if cross-module dependencies fail or are unavailable.

Update endpoint report.
