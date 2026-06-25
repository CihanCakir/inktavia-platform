# Vessel Owner Identity Bulk Smoke Test Report

## Endpoint Under Test
```http
GET /api/v1/identity/admin/users/profiles/bulk?userIds=10003&userIds=10004&userIds=10007&userIds=10008
```

## Expected Request Headers
```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <admin-identity-token>
```

## Expected Response
```json
{
  "header": { "isSuccess": true, "errorCode": 0, "errorMessage": null },
  "body": [
    { "id": 11003, "userId": 10003, "firstName": "Ayşe", "lastName": "Demir", "profilePhotoUrl": null },
    { "id": 11004, "userId": 10004, "firstName": "Mehmet", "lastName": "Kaya", "profilePhotoUrl": null },
    { "id": 11007, "userId": 10007, "firstName": "Burak", "lastName": "Arslan", "profilePhotoUrl": null },
    { "id": 11008, "userId": 10008, "firstName": "Fatma", "lastName": "Çelik", "profilePhotoUrl": null }
  ]
}
```

## BFF End-to-End Smoke Test
```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?pageIndex=0&pageSize=20" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | \
  jq '.body.vessels.items[] | { id, name, ownerUserId, ownerProfileId, ownerName, ownerAvatarUrl }'
```

## Expected BFF Response (After Fix)
```json
[
  { "id": 20001, "name": "Blue Octopus",   "ownerUserId": 10003, "ownerProfileId": 11003, "ownerName": "Ayşe Demir",   "ownerAvatarUrl": null },
  { "id": 20002, "name": "Silver Wave",    "ownerUserId": 10004, "ownerProfileId": 11004, "ownerName": "Mehmet Kaya",  "ownerAvatarUrl": null },
  { "id": 20003, "name": "Golden Compass", "ownerUserId": 10007, "ownerProfileId": 11007, "ownerName": "Burak Arslan", "ownerAvatarUrl": null },
  { "id": 20004, "name": "Storm Rider",    "ownerUserId": 10008, "ownerProfileId": 11008, "ownerName": "Fatma Çelik",  "ownerAvatarUrl": null }
]
```

## Identity Direct Test — Fallback Endpoint
```bash
curl -s -X GET "http://localhost:7101/api/v1/identity/admin/users/profiles/bulk-by-profile-ids?profileIds=11003&profileIds=11004&profileIds=11007&profileIds=11008" \
  -H "Authorization: Bearer <bff-keycloak-service-token>" \
  -H "X-Aizen-User-Token: Bearer <admin-identity-token>" | jq '.'
```

## Warnings Verification
```bash
curl -s -X GET "http://localhost:<BFF_PORT>/api/v1/admin-panel/vessels?pageIndex=0&pageSize=20" \
  -H "X-Aizen-User-Token: Bearer <admin_identity_token>" | jq '.body.warnings'
```
Expected when Identity is up: `[]`
Expected when Identity is down: `[{ "module": "Identity", "message": "..." }]`

## BFF Diagnostic Log Pattern (DEBUG level)
After the fix, the following DEBUG log lines should appear when the BFF processes a vessel list request:
```
[VesselListBff] ownerUserIds collected: 4 / 10003,10004,10007,10008
[VesselListBff] Identity profile response success: True, profiles returned: 4
```

If you see `profiles returned: 0` with `success: True`, this indicates a DB seed issue. Run the profileId fallback diagnostic:
```
[VesselListBff] Falling back to profile-ID lookup for 4 items / profileIds: 11003,11004,11007,11008
[VesselListBff] Fallback profile-ID response success: True, profiles returned: 4
```

## Identity Seed Verification SQL
```sql
SELECT id, user_id, first_name, last_name, profile_photo_url
FROM identity."UserProfiles"
WHERE user_id IN (10003, 10004, 10007, 10008);
```
Expected: 4 rows. If 0 rows, check seed file path and `MockData:Enabled` in `appsettings.Local.json`.

## Notes
- Identity API port: `7101` (from `appsettings.Local.json`)
- Redis cache TTL for vessel list: 5 min — flush with `redis-cli FLUSHDB` if testing seed changes
- BFF timeout for Identity RemoteCall: 15 seconds (configured in `DependencyInjection.cs`)
