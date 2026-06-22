# Vessel List + Register Smoke Test Report

## Build Status
```bash
cd /Users/cihancakir/Desktop/Mine/DEV/addesso-project
dotnet build Aizen.sln --no-incremental
```
**Result: 855 Warning(s), 0 Error(s)** ✅

## Prerequisites for Runtime Smoke Tests
The following services must be running:
- PostgreSQL (vessel schema, identity schema)
- Redis
- Keycloak (`http://localhost:8080/realms/inktavia-realm`)
- Identity API (`http://localhost:7101`)
- Vessel API (`http://localhost:7105`)
- ReferenceData API (`http://localhost:7104`)
- AdminPanel BFF (`http://localhost:<BFF_PORT>`)

## Smoke Test 1 — Vessel List Enrichment
```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?pageIndex=0&pageSize=20" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | \
  jq '.body.vessels.items[] | {id,name,ownerName,lastLocationText,latitude,longitude,operationalStatus,operationalStatusLabel,status,statusLabel}'
```

**Expected result:**
- `ownerName` populated for vessels with a primary owner
- `lastLocationText` = marina name or `"lat, lon"` for vessels with location snapshots
- `operationalStatusLabel`, `assetTypeLabel`, `ownershipStatusLabel`, `statusLabel` populated
- `warnings` empty if all modules up

## Smoke Test 2 — Register Bootstrap (GET)
```bash
curl -i -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/register" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>"
```

**Expected result:**
- HTTP `200 OK` (was `404` before fix)
- Body contains `vesselRegister.defaults` and `vesselRegister.options`
- `assetTypes`, `operationalStatuses`, `ownershipStatuses`, `vesselTypes`, `hullMaterials` populated (static)
- `flagCountries`, `buildCountries` populated from ReferenceData
- `ownerCandidates` populated from Identity

## Smoke Test 3 — Register Create (POST)
```bash
curl -s -X POST "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/register" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Vessel Smoke",
    "vesselTypeCode": "MOTOR_YACHT",
    "flagCountryCode": "TR",
    "ownerUserId": 10001
  }'
```

**Expected result:**
- HTTP `200 OK`
- Body contains `vessel.id` (long), `vessel.vesselCode`, `vessel.name`

## Smoke Test 4 — Auth Guards
```bash
# No token → 401
curl -i -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/register"
# Expected: 401

# Customer token → 403
curl -i -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels/register" \
  -H "X-Aizen-User-Token: Bearer <customer_identity_token>"
# Expected: 403
```

## Smoke Test 5 — Bulk Owner Enrichment Isolation
```bash
# With Identity down (stop Identity API), vessel list should still return with warnings
curl -s "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?pageIndex=0&pageSize=5" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | jq '.body.warnings'
# Expected: warning entry for "Identity"
# Vessel rows present with null ownerName
```

## Notes
- Runtime tests require a valid admin identity token obtained via `POST /api/v1/auth/login/username` or equivalent.
- Replace `<BFF_PORT>`, `<admin_identity_token>`, `<customer_identity_token>` with real values.
- Build smoke test confirms compilation correctness; full runtime tests require infrastructure.
