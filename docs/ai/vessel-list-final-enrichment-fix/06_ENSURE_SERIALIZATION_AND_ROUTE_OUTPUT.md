# 06 — Ensure Serialization and Route Output

## Goal

Ensure the controller returns the enriched BFF DTO, not stale/raw module DTOs.

## Required JSON Property Names

Check that `VesselListItemBffDto` serializes these names correctly:

```text
ownerName
ownerAvatarUrl
lastLocationText
operationalStatus
operationalStatusLabel
assetType
assetTypeLabel
ownershipStatus
ownershipStatusLabel
status
statusLabel
thumbnailUrl
```

## Checks

- No duplicate stale `VesselListItemBffDto` exists in another namespace.
- `AdminVesselsController` returns the enriched BFF response produced by `GetAdminVesselListBffQueryHandler`.
- `GetAdminVesselListBffQueryHandler` maps every new field after enrichment.
- `System.Text.Json` default null omission is acceptable for null values, but computed successful values must appear.
- Do not change the public route path.

Target route remains:

```http
GET /api/v1/admin-panel/vessels?pageIndex=0&pageSize=20
```
