# 00 — Master Prompt

You are fixing the final backend/BFF gaps in the Inktavia Marine OS AdminPanel Vessel list response.

Current state:

```http
GET /api/v1/admin-panel/vessels?pageIndex=0&pageSize=20
```

The endpoint returns successful paginated data with `count = 8`, but the React Admin Vessel table still has missing values.

Current response contains:

- `ownerUserId`
- `ownerProfileId`
- `lengthMeters`
- `ownershipStatus`
- `ownershipStatusLabel`
- `status`
- `statusLabel`
- partial `latitude`, `longitude`, `lastPositionDate`, `lastLocationText`

But it is missing or partially missing:

- `ownerName`
- `ownerAvatarUrl`
- `operationalStatus`
- `operationalStatusLabel`
- `assetType`
- `assetTypeLabel`
- `thumbnailUrl` / `coverMediaUrl`
- `lastLocationText` for vessels `20005`–`20008`

## Goal

Fix the backend/BFF so the Vessel list page can render Owner, Type/Length, Last Location, Status, and thumbnail fallback data correctly.

## Non-negotiable Rules

1. Do not change frontend code.
2. Do not use Guid for business entity IDs.
3. Use `long` / `long?` for user/profile/vessel/media/document/service request IDs.
4. Do not break existing register GET/POST endpoints.
5. Do not add N+1 Identity calls.
6. Do not move Identity/Profile logic into Vessel module.
7. BFF performs cross-module enrichment.
8. Vessel module only exposes IDs and vessel-owned read fields.
9. Do not fail the vessel list if Identity or optional enrichment fails.
10. Preserve AdminPanelAccess and BFF service-token model.
11. Build must pass with 0 errors.

Execute prompts `01` through `08` in order.
