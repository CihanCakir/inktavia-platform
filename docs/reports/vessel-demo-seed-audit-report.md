# Vessel Demo Seed Audit Report

**Date**: 2026-06-19  
**Branch**: feature/service-request-registration  
**Scope**: Entity ID type audit, stable ID manifest, and local-only constant documentation for Vessel demo seed data

---

## 1. Entity ID Type Discovery

All ID types confirmed from source code inspection of `Aizen.Modules.Vessel.Domain`:

| Entity | ID Property | Type | FK Properties | FK Type |
|--------|-------------|------|---------------|---------|
| VesselEntity | Id | `long` | — | — |
| VesselOwnerEntity | Id | `long` | VesselId, UserId, UserProfileId | `long`, `long`, `long?` |
| VesselSpecificationEntity | Id | `long` | VesselId | `long` |
| VesselEngineEntity | Id | `long` | VesselId | `long` |
| VesselMediaEntity | Id | `long` | VesselId, UploadedByUserId | `long`, `long?` |
| VesselMediaEntity | FileId | `Guid?` | _(FileStorage integration — proven Guid in source)_ | `Guid?` |
| VesselDocumentEntity | Id | `long` | VesselId, ApprovedByUserId | `long`, `long?` |
| VesselDocumentEntity | FileId | `Guid?` | _(FileStorage integration — proven Guid in source)_ | `Guid?` |
| VesselLocationSnapshotEntity | Id | `long` | VesselId | `long` |

**Note**: `FileId` on `VesselMediaEntity` and `VesselDocumentEntity` is `Guid?`, proven by source code. No other Guid IDs exist. For local demo seed, `FileId` is set to `null` (no real FileStorage objects).

---

## 2. Stable ID Ranges (Cross-Module Manifest)

Pre-existing allocations (from `mock-data-cross-module-id-manifest-report.md`):

| Entity Group | ID Range |
|-------------|----------|
| Identity Users | 10001–10015 |
| Identity UserProfiles | 11001–11015 |
| Vessels | 20001–20010 |
| VesselOwners | 21001–21015 |
| VesselSpecifications | 22001–22010 |
| VesselEngines | 23001–23010 |

**New allocations added in this implementation**:

| Entity Group | ID Range | Count |
|-------------|----------|-------|
| VesselMedia | 24001–24012 | 12 |
| VesselDocuments | 25001–25016 | 16 |
| VesselLocationSnapshots | 26001–26004 | 4 |

Sequence advance target: `100000` for all vessel tables after seed.

---

## 3. User/Profile ID Reuse

All user and profile IDs are reused from the existing Identity mock data seed:

| Constant | Value | Identity Reference |
|----------|-------|-------------------|
| `AdminUserId` | `10001` | admin@inktavia.local |
| `BoatOwnerUserId_Ayse` | `10003` | ayse.demir@inktavia.local |
| `BoatOwnerUserId_Mehmet` | `10004` | mehmet.kaya@inktavia.local |
| `BoatOwnerUserId_Burak` | `10007` | burak.arslan@inktavia.local |
| `BoatOwnerUserId_Fatma` | `10008` | fatma.celik@inktavia.local |

No new long ID constants were introduced beyond the ID ranges above. All user references reuse the Identity mock data IDs.

---

## 4. Duplicate Guard Strategy

| Entity | Guard Field(s) | Implementation |
|--------|---------------|----------------|
| Vessel | `Id` | `AnyAsync(v => v.Id == model.Id)` |
| VesselOwner | `Id` | `AnyAsync(o => o.Id == model.Id)` |
| VesselSpecification | `Id` | `AnyAsync(s => s.Id == model.Id)` |
| VesselEngine | `Id` | `AnyAsync(e => e.Id == model.Id)` |
| VesselMedia | `Id` | `AnyAsync(m => m.Id == model.Id)` |
| VesselDocument | `Id` | `AnyAsync(d => d.Id == model.Id)` |
| VesselLocationSnapshot | `Id` | `AnyAsync(l => l.Id == model.Id)` |

All checks use explicit stable numeric IDs to ensure idempotency. Re-running the seeder is safe — existing records are skipped.

---

## 5. Environment Guard

```json
"MockData": {
  "Enabled": true,
  "RunOnStartup": true,
  "EnvironmentGuard": ["Local", "Development"],
  "SeedMode": "InsertMissingOnly",
  "DataSet": "admin-demo"
}
```

Seeder never runs in `Production` or `Staging`. The `appsettings.Local.json` enables it for local development.

---

## 6. Files Created/Modified

| File | Action |
|------|--------|
| `Modules/Vessel/src/.../Seed/Json/MockData/admin-demo/vessel-media.json` | Created |
| `Modules/Vessel/src/.../Seed/Json/MockData/admin-demo/vessel-documents.json` | Created |
| `Modules/Vessel/src/.../Seed/Json/MockData/admin-demo/vessel-location-snapshots.json` | Created |
| `Modules/Vessel/src/.../Seed/MockData/VesselMockDataSeeder.cs` | Modified |
