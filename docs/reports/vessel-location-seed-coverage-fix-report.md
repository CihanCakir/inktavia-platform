# Vessel Location Seed Coverage Fix Report

## Scope

Add missing location snapshots for seeded vessels 20005–20008 so all 8 demo vessels return `lastLocationText` in the Vessel list response.

---

## Root Cause

`vessel-location-snapshots.json` only contained entries 26001–26004 (for vessels 20001–20004). Vessels 20005–20008 had no associated location snapshots. The `GetAllVesselsAdminQueryHandler` EF projection filters by `IsCurrent && IsActive`, so vessels with no snapshot returned `null` for latitude, longitude, lastPositionDate, and lastLocationText.

---

## Files Changed

### `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-location-snapshots.json`

Added 4 new entries:

```json
{ "id": 26005, "vesselId": 20005, "countryCode": "TR", "cityCode": "MUGLA",
  "marinaName": "Bodrum Marina", "latitude": 37.0344, "longitude": 27.4305,
  "accuracyMeters": 50.0, "source": "Manual", "capturedAt": "2026-06-19T10:00:00Z", "isCurrent": true },
{ "id": 26006, "vesselId": 20006, "countryCode": "TR", "cityCode": "ISTANBUL",
  "marinaName": "Kalamış Marina", "latitude": 40.9693, "longitude": 29.0523,
  "accuracyMeters": 50.0, "source": "Manual", "capturedAt": "2026-06-19T10:00:00Z", "isCurrent": true },
{ "id": 26007, "vesselId": 20007, "countryCode": "TR", "cityCode": "MUGLA",
  "marinaName": "Yalıkavak Marina", "latitude": 37.1067, "longitude": 27.2844,
  "accuracyMeters": 50.0, "source": "Manual", "capturedAt": "2026-06-19T10:00:00Z", "isCurrent": true },
{ "id": 26008, "vesselId": 20008, "countryCode": "TR", "cityCode": "MUGLA",
  "marinaName": "Göcek Marina", "latitude": 36.7530, "longitude": 28.9390,
  "accuracyMeters": 50.0, "source": "Manual", "capturedAt": "2026-06-19T10:00:00Z", "isCurrent": true }
```

---

## Seeder Behavior

`VesselLocationSnapshotEntity.Create(...)` sets `IsCurrent = true` and `IsActive = true` by default.
The seeder guards by ID (`if AnyAsync(l => l.Id == model.Id)`), so entries 26005–26008 are inserted exactly once.

---

## LastLocationText Logic

Priority (BFF handler `ComputeLastLocationText`):
1. `marinaName` if non-empty → returns marina name
2. `latitude, longitude` if both present → returns formatted coordinates
3. null

With these snapshots, all 8 vessels will return `lastLocationText` as the marina name.

---

## Inserted Counts (on next startup with missing data)

| Action | Count |
|--------|-------|
| Inserted | 4 (IDs 26005–26008) |
| Skipped | 4 (IDs 26001–26004 already exist) |

---

## Expected After Fix

All 8 seeded vessels return non-null `lastLocationText`:

| VesselId | lastLocationText   |
|----------|--------------------|
| 20001    | Kalamış Marina     |
| 20002    | Göcek Marina       |
| 20003    | Fenerbahçe Marina  |
| 20004    | Çeşme Marina       |
| 20005    | Bodrum Marina      |
| 20006    | Kalamış Marina     |
| 20007    | Yalıkavak Marina   |
| 20008    | Göcek Marina       |

---

## Build Validation

0 errors. No code changes required; seeder + JSON only.
