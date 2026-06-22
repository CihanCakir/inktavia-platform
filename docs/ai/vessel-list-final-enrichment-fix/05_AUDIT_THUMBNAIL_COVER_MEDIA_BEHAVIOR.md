# 05 — Audit Thumbnail / Cover Media Behavior

## Problem

`thumbnailUrl` / `coverMediaUrl` is missing in the Vessel list response.

## Known Constraint

Local seed records may have `FileId = null`, so FileStorage signed URL may not exist. Do not require FileStorage for local demo list thumbnails.

## Expected Behavior

If a vessel has seeded cover/primary media and the schema supports a URL-like field, expose a usable local/demo URL. If the schema only stores `FileId`, document the gap and preserve frontend fallback.

## Audit

```text
VesselMediaEntity
Vessel media seed JSON
Vessel list projection
VesselListItemDto
VesselListItemBffDto
GetAdminVesselListBffQueryHandler
```

## Implementation Options, in Order

1. If `VesselMediaEntity` has `Url`, `AccessUrl`, `PublicUrl`, `ThumbnailUrl`, or equivalent, project cover media URL from the primary/cover media row.
2. If only `FileId` exists and it is non-null, call FileStorage signed URL only if the current BFF already has a safe FileStorage RemoteCall pattern.
3. If local demo seed has `FileId = null` and no URL field exists, leave `thumbnailUrl = null`, add a final report gap, and do not add fake schema fields.

## Rules

- Do not add new schema fields just to satisfy thumbnail display unless current entity already has those fields or a prior migration supports them.
- Do not call FileStorage if there is no existing safe BFF RemoteCall pattern.
- Do not break list endpoint if thumbnail enrichment fails.
