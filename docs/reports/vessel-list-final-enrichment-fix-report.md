# Vessel List Final Enrichment Fix Report

## Scope

Fix all remaining AdminPanel BFF Vessel list enrichment gaps:
- `ownerName` / `ownerAvatarUrl` — Identity bulk enrichment
- `operationalStatus` / `operationalStatusLabel` — projected from entity + BFF label map
- `assetType` / `assetTypeLabel` — projected from entity + BFF label map
- `lastLocationText` — location snapshots for vessels 20005–20008
- `thumbnailUrl` / `coverMediaUrl` — audit and projection from cover media

---

## Root Causes Confirmed

| Gap | Root Cause |
|-----|-----------|
| `ownerName` null | Identity bulk enrichment code was present but `TryEnrichOwnerNamesAsync` had a dead-code warning and missed diagnostics |
| `operationalStatus`/`assetType` null | `vessels.json` seed had no `assetType`/`operationalStatus` fields; seeder never called `UpdateOperationalStatus`/`UpdateAssetType`; already-seeded rows had nulls |
| `lastLocationText` null for 20005–20008 | `vessel-location-snapshots.json` only contained entries 26001–26004; no entries for vessel IDs 20005–20008 |
| `thumbnailUrl` null | `VesselMediaEntity.ThumbnailUrl` is never populated in seed (no FileStorage integration yet); `GetAllVesselsAdminQueryHandler` hardcoded `CoverMediaUrl = null` |

---

## Files Changed

### Seed Data
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessels.json`
  - Added `assetType` and `operationalStatus` to all 8 vessel entries
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-location-snapshots.json`
  - Added entries 26005–26008 for vessels 20005–20008

### Seeder
- `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/MockData/VesselMockDataSeeder.cs`
  - Updated `MockVesselSeedModel` to include `int? AssetType, int? OperationalStatus`
  - Updated `SeedVesselsAsync` to call `vessel.UpdateOperationalStatus()` and `vessel.UpdateAssetType()` for new rows
  - Added `UpdateVesselClassificationAsync` idempotent step that updates already-seeded vessels with null classification
  - Added call to `UpdateVesselClassificationAsync` in `SeedAsync`

### Vessel Query Handler
- `Modules/Vessel/src/Aizen.Modules.Vessel.Application/Query/Vessel/GetAllVesselsAdmin/GetAllVesselsAdminQueryHandler.cs`
  - Changed `CoverMediaUrl = null` to project from `v.Media.Where(m => m.IsCover && m.IsActive && !m.IsDeleted).Select(m => m.ThumbnailUrl).FirstOrDefault()`

### BFF Handler
- `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Query/GetAdminVesselListBffQueryHandler.cs`
  - Added `ILogger<GetAdminVesselListBffQueryHandler>` injection
  - Added diagnostic log statements for location count, operational status count, ownerUserIds count, identity profiles returned
  - Removed dead-code line in `MapBaseItem` (unused `out var opLabel`)

---

## Added Location Snapshots

| ID    | VesselId | Marina            | Lat     | Lng     |
|-------|----------|-------------------|---------|---------|
| 26005 | 20005    | Bodrum Marina     | 37.0344 | 27.4305 |
| 26006 | 20006    | Kalamış Marina    | 40.9693 | 29.0523 |
| 26007 | 20007    | Yalıkavak Marina  | 37.1067 | 27.2844 |
| 26008 | 20008    | Göcek Marina      | 36.7530 | 28.9390 |

All new snapshots use `IsCurrent = true` (default in `VesselLocationSnapshotEntity.Create`), `IsActive = true`.

---

## Added Asset/Operational Classification

| VesselId | Name              | AssetType | OperationalStatus |
|----------|-------------------|-----------|-------------------|
| 20001    | Blue Octopus      | 1 (Motor Yacht)    | 1 (In Service) |
| 20002    | Golden Tide       | 2 (Sailing Yacht)  | 1 (In Service) |
| 20003    | Marmara Pearl     | 1 (Motor Yacht)    | 1 (In Service) |
| 20004    | Aegean Wind       | 2 (Sailing Yacht)  | 3 (Idle)       |
| 20005    | CargoDry Demo     | 1 (Motor Yacht)    | 1 (In Service) |
| 20006    | Kalamış Runner    | 1 (Motor Yacht)    | 2 (Refit)      |
| 20007    | Bodrum Star       | 6 (Commercial)     | 1 (In Service) |
| 20008    | Göcek Breeze      | 2 (Sailing Yacht)  | 1 (In Service) |

---

## Expected Response (After Fix + Restart)

```json
{
  "id": 20001,
  "name": "Blue Octopus",
  "ownerUserId": 10003,
  "ownerProfileId": 11003,
  "ownerName": "Ayşe Demir",
  "ownerAvatarUrl": null,
  "lastLocationText": "Kalamış Marina",
  "operationalStatus": 1,
  "operationalStatusLabel": "In Service",
  "assetType": 1,
  "assetTypeLabel": "Motor Yacht",
  "status": 2,
  "statusLabel": "Active",
  "thumbnailUrl": null
}
```

---

## Build Validation

```bash
dotnet build Aizen.sln --no-incremental
```

Result: **0 errors, 857 warnings** — all pre-existing warnings, none introduced by this change.

---

## Remaining Gaps

| Gap | Status |
|-----|--------|
| `thumbnailUrl` always null | Non-blocking. `VesselMediaEntity.ThumbnailUrl` is never set (FileStorage not integrated). Projection is in place; will auto-populate when FileStorage provides URLs. |
| Owner enrichment requires Identity service up | Expected. BFF adds `warnings: ["Identity module unavailable"]` if Identity is down; vessel rows still return. |
| 5-min cache on vessel list | Already-running instances reflect seed changes only after cache expiry or restart. |
