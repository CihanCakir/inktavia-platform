# Current Live Response Gaps

The current backend response for:

```http
GET /api/v1/admin-panel/vessels?pageIndex=0&pageSize=20
```

returns successful paginated data:

```json
{
  "header": { "isSuccess": true, "errorCode": 0 },
  "body": {
    "vessels": {
      "from": 0,
      "index": 0,
      "size": 20,
      "count": 8,
      "pages": 1,
      "hasPrevious": false,
      "hasNext": false,
      "items": []
    },
    "warnings": []
  }
}
```

The following fields are present in the item response:

- `ownerUserId`
- `ownerProfileId`
- `lengthMeters`
- `ownershipStatus`
- `ownershipStatusLabel`
- `status`
- `statusLabel`
- partial `latitude`, `longitude`, `lastPositionDate`, `lastLocationText`

The following fields are missing or incomplete:

- `ownerName`
- `ownerAvatarUrl`
- `operationalStatus`
- `operationalStatusLabel`
- `assetType`
- `assetTypeLabel`
- `thumbnailUrl` / `coverMediaUrl`
- `lastLocationText` for seeded vessels `20005`, `20006`, `20007`, `20008`

## Expected Fix Focus

- Owner enrichment must happen in AdminPanel BFF via Identity bulk profile lookup.
- Operational status and asset type must be projected from Vessel module and mapped to labels in BFF.
- Location seed coverage must be extended for all 8 local demo vessels.
- Thumbnail behavior must be implemented if supported by schema, otherwise documented as a non-blocking gap.
