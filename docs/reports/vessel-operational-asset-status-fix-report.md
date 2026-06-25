# Vessel Operational Status and Asset Type Fix Report

## Scope

Fix `operationalStatus`, `operationalStatusLabel`, `assetType`, and `assetTypeLabel` missing from the AdminPanel Vessel list response.

---

## Root Cause

`vessels.json` seed file did not include `assetType` or `operationalStatus` fields. The seeder's `MockVesselSeedModel` record did not map these fields. The seeder never called `vessel.UpdateOperationalStatus()` or `vessel.UpdateAssetType()`. All 8 seeded vessels had `null` for both fields in PostgreSQL.

---

## Files Changed

### `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessels.json`

Added `assetType` and `operationalStatus` to all 8 entries:

```json
{ "id": 20001, ..., "assetType": 1, "operationalStatus": 1 }
{ "id": 20002, ..., "assetType": 2, "operationalStatus": 1 }
{ "id": 20003, ..., "assetType": 1, "operationalStatus": 1 }
{ "id": 20004, ..., "assetType": 2, "operationalStatus": 3 }
{ "id": 20005, ..., "assetType": 1, "operationalStatus": 1 }
{ "id": 20006, ..., "assetType": 1, "operationalStatus": 2 }
{ "id": 20007, ..., "assetType": 6, "operationalStatus": 1 }
{ "id": 20008, ..., "assetType": 2, "operationalStatus": 1 }
```

### `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/MockData/VesselMockDataSeeder.cs`

1. Updated `MockVesselSeedModel` record — added `int? AssetType, int? OperationalStatus` parameters.
2. In `SeedVesselsAsync` — added calls to `vessel.UpdateAssetType()` and `vessel.UpdateOperationalStatus()` for freshly created vessels.
3. Added `UpdateVesselClassificationAsync` — idempotent update step for already-seeded vessels with null classification. Runs after `SeedLocationSnapshotsAsync` on every startup.

---

## Label Dictionaries (BFF Handler)

Both label dictionaries already exist in `GetAdminVesselListBffQueryHandler`:

```csharp
{ 1, "In Service" }, { 2, "Refit" }, { 3, "Idle" }, { 4, "Decommissioned" }
{ 1, "Motor Yacht" }, { 2, "Sailing Yacht" }, { 3, "Superyacht" },
{ 4, "Catamaran" }, { 5, "RIB" }, { 6, "Commercial" }
```

Mapping in `MapBaseItem` was already present and correct.

---

## Vessel Module Projection

`GetAllVesselsAdminQueryHandler` already projects:

```csharp
OperationalStatus = v.OperationalStatus,
AssetType = v.AssetType,
```

No changes needed to the query handler for these fields.

---

## Expected After Fix

```json
{
  "operationalStatus": 1,
  "operationalStatusLabel": "In Service",
  "assetType": 1,
  "assetTypeLabel": "Motor Yacht"
}
```

---

## Seed Impact

- 8 vessels updated with classification on next startup (if already seeded)
- 0 vessels re-inserted (idempotent seeder skips existing IDs)
- `UpdateVesselClassificationAsync` applies only when current DB value differs from seed file value

---

## Build Validation

0 errors after changes.
