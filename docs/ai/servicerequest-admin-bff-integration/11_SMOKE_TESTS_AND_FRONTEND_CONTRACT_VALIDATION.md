# 11 — Smoke Tests and Frontend Contract Validation

Run build and smoke tests.

## Build

```bash
dotnet restore
dotnet build --no-incremental
```

## Runtime prerequisites

Start:
- Keycloak
- Redis
- ServiceRequest module
- Vessel module
- AdminPanel BFF

## Smoke tests

Use a valid admin Identity token.

```bash
# ServiceRequest list
curl -s "http://localhost:<BFF_PORT>/api/v1/admin-panel/service-requests?index=0&size=10" \
  -H "X-Aizen-User-Token: Bearer <identity_token>" | jq '.header.isSuccess, .body.serviceRequests.items[0]'

# By-vessel history
curl -s "http://localhost:<BFF_PORT>/api/v1/admin-panel/service-requests/by-vessel/1/history?take=10" \
  -H "X-Aizen-User-Token: Bearer <identity_token>" | jq '.header.isSuccess, .body.serviceHistory'

# Vessel Detail must include enriched ServiceRequest fields
curl -s "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/1/detail" \
  -H "X-Aizen-User-Token: Bearer <identity_token>" | jq '.header.isSuccess, .body.vessel.serviceHistory'
```

## Auth tests

- no token → 401
- participant/customer token → 403
- admin token → success

## Output

Create `docs/reports/servicerequest-bff-smoke-test-report.md`.
