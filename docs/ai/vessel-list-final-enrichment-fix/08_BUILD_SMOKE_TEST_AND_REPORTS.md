# 08 — Build, Smoke Test, and Reports

## Build

Run:

```bash
dotnet build Aizen.sln --no-incremental
```

Expected:

```text
0 errors
```

## Required Services for Smoke Test

- Identity API
- Vessel API
- ReferenceData API if needed
- AdminPanel BFF
- Keycloak
- PostgreSQL
- Redis

## Vessel List Smoke Test

```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?pageIndex=0&pageSize=20" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | \
  jq '.body.vessels.items[] | {
    id,
    name,
    ownerUserId,
    ownerProfileId,
    ownerName,
    ownerAvatarUrl,
    vesselTypeCode,
    assetType,
    assetTypeLabel,
    lengthMeters,
    latitude,
    longitude,
    lastPositionDate,
    lastLocationText,
    operationalStatus,
    operationalStatusLabel,
    ownershipStatus,
    ownershipStatusLabel,
    status,
    statusLabel,
    thumbnailUrl
  }'
```

Expected:

- `count = 8`
- `ownerName` is populated for all vessels with valid owner user IDs
- `lastLocationText` is populated for all 8 seeded vessels after seed update
- `operationalStatus` and `operationalStatusLabel` are populated
- `assetType` and `assetTypeLabel` are populated
- `statusLabel` remains populated
- `warnings = []` when all modules are running
- If Identity is down, `ownerName = null` and warnings include Identity, but vessel rows remain

## Direct Identity Bulk Test

```bash
curl -s -X GET "http://localhost:<IDENTITY_PORT>/api/v1/identity/admin/users/profiles/bulk?userIds=10003&userIds=10004&userIds=10007&userIds=10008" \
  -H "Authorization: Bearer <admin_or_service_token>" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | jq '.'
```

If this route fails, fix either the Identity route or the BFF RemoteCall route.

## Reports

Create:

```text
docs/reports/vessel-list-final-enrichment-fix-report.md
docs/reports/vessel-owner-identity-bulk-runtime-validation-report.md
docs/reports/vessel-operational-asset-status-fix-report.md
docs/reports/vessel-location-seed-coverage-fix-report.md
docs/reports/vessel-thumbnail-covermedia-gap-report.md
docs/reports/vessel-list-final-smoke-test-report.md
```

Each report must include:

- files changed
- root cause confirmed
- exact route tested
- exact before/after response samples
- inserted/skipped seed counts if seed changed
- whether Identity bulk route works
- whether ownerName/ownerAvatarUrl is populated
- whether operationalStatus/assetType labels are populated
- remaining gaps
