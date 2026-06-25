# Vessel Thumbnail / Cover Media Gap Report

## Scope

Audit `thumbnailUrl` / `coverMediaUrl` behavior in the AdminPanel Vessel list and document the current state and gap.

---

## Entity Audit

`VesselMediaEntity` has a `ThumbnailUrl` field:

```csharp
public string? ThumbnailUrl { get; private set; }
```

This field is present in the schema. However:
- The `Create(...)` factory method does not accept or set `ThumbnailUrl`
- The seeder (`VesselMockDataSeeder`) never sets `ThumbnailUrl`
- No migration sets a default value
- FileStorage integration (pre-signed URLs) is not yet active

---

## Query Handler Fix Applied

`GetAllVesselsAdminQueryHandler` was updated from:

```csharp
CoverMediaUrl = null,
```

to:

```csharp
CoverMediaUrl = v.Media
    .Where(m => m.IsCover && m.IsActive && !m.IsDeleted)
    .Select(m => m.ThumbnailUrl)
    .FirstOrDefault(),
```

This projection is now in place. When `ThumbnailUrl` is populated (via FileStorage integration), the Vessel list will automatically return it without further code changes.

---

## BFF Mapping

`GetAdminVesselListBffQueryHandler.MapBaseItem`:

```csharp
ThumbnailUrl = v.CoverMediaUrl,
```

`VesselListItemBffDto.ThumbnailUrl` is exposed as a nullable string. Frontend receives `null` and should use a placeholder avatar/image.

---

## Current State

| Vessel | Has Cover Media Record | ThumbnailUrl |
|--------|------------------------|--------------|
| 20001  | Yes (id: 24001)        | null (not set by FileStorage) |
| 20002  | Yes (id: 24004)        | null |
| 20003  | Yes (id: 24007)        | null |
| 20004  | No media records       | null |
| 20005  | Yes (id: 24010)        | null |
| 20006  | No media records       | null |
| 20007  | No media records       | null |
| 20008  | No media records       | null |

---

## Non-Blocking Status

`thumbnailUrl = null` is the correct and expected value in the current local dev environment. The frontend must use a fallback image when `thumbnailUrl` is null.

---

## Required Follow-up

1. **FileStorage Integration**: When FileStorage module generates pre-signed read URLs for vessel media, populate `VesselMediaEntity.ThumbnailUrl` via a domain method or FileStorage callback.
2. **Seed ThumbnailUrl**: Consider adding a `SetThumbnailUrl(string url)` method to `VesselMediaEntity` for FileStorage to call after processing.
3. **Media seed for vessels 20004, 20006, 20007, 20008**: These vessels have no media records in `vessel-media.json`. Add entries if demo thumbnails are needed.

---

## Risk

**Low** — `thumbnailUrl = null` is gracefully handled by frontend. No data loss. No broken queries.
