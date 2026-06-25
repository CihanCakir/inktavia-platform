# BFF Endpoint Contract Alignment

Implement the 8 source contract capabilities using the current AdminPanel BFF route convention.

## MVP endpoints

```text
GET /api/v1/admin-panel/vessels
GET /api/v1/admin-panel/vessels/{id}
GET /api/v1/admin-panel/vessels/{id}/documents
GET /api/v1/admin-panel/vessels/{id}/media
```

## Post-MVP or feature-gated endpoints

```text
POST /api/v1/admin-panel/vessels/{id}/documents/{documentId}/approve
POST /api/v1/admin-panel/vessels/{id}/documents/{documentId}/replace
POST /api/v1/admin-panel/vessels
GET /api/v1/admin-panel/vessels/export
```

## List query parameters

- `index`
- `size`
- `search`
- `assetTypes`
- `ownershipStatuses`
- `operationalStatuses`

## Response rule

All returned body shapes must match the source contract semantics, but class/property names must follow existing BFF style. Use body wrappers:

```json
{ "vessels": { ...page... } }
{ "vessel": { ...detail... } }
{ "documents": [ ... ] }
{ "media": [ ... ] }
```

## Export rule

If export support is not already present, implement a clear `501 Not Implemented` or feature-gated response and document it. Do not fake binary Excel output.
