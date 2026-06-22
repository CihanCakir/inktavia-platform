# Vessel List Final Smoke Test Report

## Smoke Test Command

```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?pageIndex=0&pageSize=20" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | \
  jq '.body.vessels.items[] | {
    id, name,
    ownerUserId, ownerProfileId, ownerName, ownerAvatarUrl,
    vesselTypeCode, assetType, assetTypeLabel, lengthMeters,
    latitude, longitude, lastPositionDate, lastLocationText,
    operationalStatus, operationalStatusLabel,
    ownershipStatus, ownershipStatusLabel,
    status, statusLabel, thumbnailUrl
  }'
```

## Required Services

- Identity API (`http://localhost:7101`)
- Vessel API (`http://localhost:7105`)
- AdminPanel BFF (`http://localhost:<BFF_PORT>`)
- Keycloak (`http://localhost:8080`)
- PostgreSQL
- Redis

---

## Expected Results After Fix

### Pagination
```json
{
  "count": 8,
  "pages": 1,
  "hasPrevious": false,
  "hasNext": false
}
```

### Per Item (example: vessel 20001)
```json
{
  "id": 20001,
  "name": "Blue Octopus",
  "ownerUserId": 10003,
  "ownerProfileId": 11003,
  "ownerName": "Ayşe Demir",
  "ownerAvatarUrl": null,
  "vesselTypeCode": "MOTOR_YACHT",
  "assetType": 1,
  "assetTypeLabel": "Motor Yacht",
  "lengthMeters": 13.8,
  "latitude": 40.9693,
  "longitude": 29.0523,
  "lastPositionDate": "2025-06-01T08:00:00Z",
  "lastLocationText": "Kalamış Marina",
  "operationalStatus": 1,
  "operationalStatusLabel": "In Service",
  "ownershipStatus": 2,
  "ownershipStatusLabel": "Charter",
  "status": 2,
  "statusLabel": "Active",
  "thumbnailUrl": null
}
```

### Warnings (all services up)
```json
"warnings": []
```

### Warnings (Identity down)
```json
"warnings": ["Identity module unavailable"]
```
Vessel rows are still returned with `ownerName: null`.

---

## Identity Bulk Direct Test

```bash
curl -s -X GET "http://localhost:7101/api/v1/identity/admin/users/profiles/bulk?userIds=10003&userIds=10004&userIds=10007&userIds=10008" \
  -H "Authorization: Bearer <admin_or_service_token>" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | jq '.'
```

Expected: `header.isSuccess: true`, body contains list of profile objects with `firstName`, `lastName`, `userId`.

---

## Field Coverage Checklist

| Field | Status |
|-------|--------|
| `ownerName` | ✅ Populated via Identity bulk enrichment |
| `ownerAvatarUrl` | ✅ null (no ProfilePhotoUrl in seed) — expected |
| `operationalStatus` | ✅ Populated from entity (after seeder update) |
| `operationalStatusLabel` | ✅ Mapped in BFF handler |
| `assetType` | ✅ Populated from entity (after seeder update) |
| `assetTypeLabel` | ✅ Mapped in BFF handler |
| `lastLocationText` | ✅ All 8 vessels (after seed update) |
| `statusLabel` | ✅ Existing |
| `ownershipStatusLabel` | ✅ Existing |
| `thumbnailUrl` | ⚠️ null — FileStorage not integrated, expected |

---

## Build Result

```
dotnet build Aizen.sln --no-incremental
0 Error(s), 857 Warning(s)
```

All warnings are pre-existing. No new errors or warnings introduced.

---

## Notes

- The vessel list query handler uses a 5-minute distributed cache. After applying seed changes, restart the Vessel API service (or wait for cache expiry) for classification and location data to reflect.
- Owner enrichment reflects immediately (Identity call per BFF request page, not cached separately).
