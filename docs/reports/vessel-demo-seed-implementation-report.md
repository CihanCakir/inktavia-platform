# Vessel Demo Seed Implementation Report

**Date**: 2026-06-19  
**Branch**: feature/service-request-registration  
**Scope**: Implementation of VesselMedia, VesselDocument, and VesselLocationSnapshot mock seed data for local/dev Vessel UI and AdminPanel BFF smoke tests

---

## 1. Problem Statement

The AdminPanel BFF Vessel endpoints were returning successful HTTP 200 responses but with empty lists for documents, media, and location snapshots. The existing `VesselMockDataSeeder` only seeded vessels, owners, specifications, and engines. Three additional entity types needed demo seed data.

---

## 2. Files Modified

### VesselMockDataSeeder.cs
**Path**: `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/MockData/VesselMockDataSeeder.cs`

**Changes**:
- Added `SeedMediaAsync()` method
- Added `SeedDocumentsAsync()` method
- Added `SeedLocationSnapshotsAsync()` method
- Updated `SeedAsync()` to call three new methods
- Updated `AdvanceSequencesAsync()` to advance sequences for `vessel_media`, `vessel_documents`, and `vessel_location_snapshots` tables
- Added seed model records: `MockVesselMediaSeedModel`, `MockVesselDocumentSeedModel`, `MockVesselLocationSnapshotSeedModel`

---

## 3. JSON Seed Files Created

### vessel-media.json
**Path**: `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-media.json`

| ID | VesselId | Vessel Name | MediaType | IsCover |
|----|----------|-------------|-----------|---------|
| 24001 | 20001 | Blue Octopus | Photo (1) | true |
| 24002 | 20001 | Blue Octopus | Photo (1) | false |
| 24003 | 20001 | Blue Octopus | Video (2) | false |
| 24004 | 20002 | Golden Tide | Photo (1) | true |
| 24005 | 20002 | Golden Tide | Photo (1) | false |
| 24006 | 20002 | Golden Tide | Video (2) | false |
| 24007 | 20003 | Marmara Pearl | Photo (1) | true |
| 24008 | 20003 | Marmara Pearl | Photo (1) | false |
| 24009 | 20003 | Marmara Pearl | Video (2) | false |
| 24010 | 20005 | CargoDry Demo | Photo (1) | true |
| 24011 | 20005 | CargoDry Demo | Photo (1) | false |
| 24012 | 20005 | CargoDry Demo | Video (2) | false |

**Notes**:
- `FileId` is `null` for all seed records (no real FileStorage objects)
- `ContentTypeSnapshot` set to `image/jpeg` or `video/mp4`
- `SizeInBytesSnapshot` set to realistic values

### vessel-documents.json
**Path**: `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-documents.json`

4 documents per vessel for vessels 20001, 20002, 20003, 20005.

| DocumentTypeCode | ExpiresAt | DocumentStatus | Notes |
|-----------------|-----------|----------------|-------|
| REGISTRATION_CERTIFICATE | 2027-06-30 | Active (1) | Long-term valid |
| INSURANCE_POLICY | ~90 days from now | PendingRenewal (4) | Expiring soon |
| SAFETY_EQUIPMENT | 2024-12-31 | Expired (2) | Already expired |
| RADIO_LICENSE | null | Active (1) | No expiry |

**Notes**:
- `FileId` is `null` for all seed records
- `DocumentStatus` is set via `ChangeStatus()` after `Create()` — respects entity domain methods
- `IssuingAuthority` is stored in JSON but not set on entity (no public setter / domain method exists) — future gap

### vessel-location-snapshots.json
**Path**: `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-location-snapshots.json`

| ID | VesselId | Marina | Latitude | Longitude |
|----|----------|--------|----------|-----------|
| 26001 | 20001 | Kalamış Marina | 40.9693 | 29.0523 |
| 26002 | 20002 | Göcek Marina | 36.7530 | 28.9390 |
| 26003 | 20003 | Fenerbahçe Marina | 40.9782 | 29.0429 |
| 26004 | 20004 | Çeşme Marina | 38.3244 | 26.3011 |

---

## 4. Seeder Registration

The seeder is registered via `AddVesselMockData()` in `DependencyInjection.cs` and called from `SeedVesselAsync()` which runs at startup in `Program.cs`. No changes were required to the registration pattern.

**Enabled via `appsettings.Local.json`**:
```json
"MockData": {
  "Enabled": true,
  "RunOnStartup": true,
  "EnvironmentGuard": ["Local", "Development"],
  "DataSet": "admin-demo"
}
```

---

## 5. Expected Log Output (first run)

```
[VesselDemoSeed] Inserted media: 12, skipped: 0
[VesselDemoSeed] Inserted documents: 16, skipped: 0
[VesselDemoSeed] Inserted location snapshots: 4, skipped: 0
```

**Subsequent runs** (idempotent):
```
[VesselDemoSeed] Inserted media: 0, skipped: 12
[VesselDemoSeed] Inserted documents: 0, skipped: 16
[VesselDemoSeed] Inserted location snapshots: 0, skipped: 4
```

---

## 6. Build Validation

```
Build succeeded.
96 Warning(s) — pre-existing nullability warnings, unrelated to seed changes
0 Error(s)
```

Command: `dotnet build Modules/Vessel/src/Aizen.Modules.Vessel/Aizen.Modules.Vessel.csproj --no-restore`

---

## 7. Remaining Gaps

| Gap | Severity | Notes |
|-----|----------|-------|
| `IssuingAuthority` not seeded on VesselDocumentEntity | Low | No public setter or domain method exists. Can be added via entity Update() extension if needed. |
| `Title`/`Description`/`ThumbnailUrl` not seeded on VesselMediaEntity | Low | Not in Create() factory. Can be added to entity or set via admin commands. |
| `FileId` is null for all media/document seed records | Medium | FileStorage objects don't exist for demo data. BFF `AccessUrl` will be null for seeded records. |
| Document version history not seeded | Low | `VesselDocumentEntity` does not have a version sub-entity in current schema. |

---

## 8. Rollback Instructions

To remove local demo seed data from the database:

```sql
DELETE FROM vessel.vessel_location_snapshots WHERE id BETWEEN 26001 AND 26004;
DELETE FROM vessel.vessel_documents WHERE id BETWEEN 25001 AND 25016;
DELETE FROM vessel.vessel_media WHERE id BETWEEN 24001 AND 24012;
DELETE FROM vessel.vessel_engines WHERE id BETWEEN 23001 AND 23010;
DELETE FROM vessel.vessel_specifications WHERE id BETWEEN 22001 AND 22010;
DELETE FROM vessel.vessel_owners WHERE id BETWEEN 21001 AND 21015;
DELETE FROM vessel.vessels WHERE id BETWEEN 20001 AND 20010;
```

Or simply drop and recreate the local database and re-run migrations.
