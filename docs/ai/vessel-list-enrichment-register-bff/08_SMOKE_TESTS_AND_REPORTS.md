# 08 — Smoke Tests and Reports

Run build:

```bash
dotnet restore
dotnet build --no-incremental
```

Then start:

- Identity API
- Vessel API
- ReferenceData API if register options depend on it
- AdminPanel BFF
- Keycloak
- PostgreSQL
- Redis

## Smoke tests

### 1. Vessel list enrichment

```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?pageIndex=0&pageSize=20" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | jq '.body.vessels.items[] | {id,name,ownerName,lastLocationText,latitude,longitude,operationalStatus,operationalStatusLabel,status,statusLabel}'
```

Expected:

- at least one item has non-null `ownerName`
- seeded vessels with location snapshots have `latitude`, `longitude`, and `lastLocationText` or coordinates
- status label fields or numeric status fields are populated

### 2. Register bootstrap

```bash
curl -i -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/register" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>"
```

Expected:

- `200 OK`
- response includes options/defaults
- no `404`

### 3. Auth guards

No token:

```bash
curl -i -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/register"
```

Expected: `401`.

Customer token:

```bash
curl -i -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/register" \
  -H "X-Aizen-User-Token: Bearer <customer_identity_token>"
```

Expected: `403`.

## Reports

Generate:

```text
docs/reports/vessel-list-enrichment-audit-report.md
docs/reports/vessel-owner-identity-bulk-enrichment-report.md
docs/reports/vessel-location-status-mapping-report.md
docs/reports/vessel-register-bootstrap-endpoint-report.md
docs/reports/vessel-register-create-endpoint-report.md
docs/reports/vessel-list-register-smoke-test-report.md
docs/reports/vessel-list-register-final-gap-report.md
```
