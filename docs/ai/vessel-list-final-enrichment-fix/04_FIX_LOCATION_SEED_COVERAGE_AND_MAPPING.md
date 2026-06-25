# 04 — Fix Location Seed Coverage and Mapping

## Problem

`lastLocationText` currently appears only for seeded vessels `20001`–`20004`. Vessels `20005`–`20008` still show null.

## Expected Behavior

All 8 local demo vessels should have a latest/current location snapshot.

## Files to Audit and Fix

```text
Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-location-snapshots.json
Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/MockData/VesselMockDataSeeder.cs
Modules/Vessel/src/Aizen.Modules.Vessel.Application/Query/Vessel/GetAllVesselsAdmin/GetAllVesselsAdminQueryHandler.cs
```

## Add Missing Snapshots

Add idempotent snapshots:

```text
26005 → VesselId 20005 → Bodrum Marina → 37.0344, 27.4305
26006 → VesselId 20006 → Kalamış Marina → 40.9693, 29.0523
26007 → VesselId 20007 → Yalıkavak Marina → 37.1067, 27.2844
26008 → VesselId 20008 → Göcek Marina → 36.7530, 28.9390
```

## Ensure

- Each snapshot is marked current/active according to existing entity fields.
- `CapturedAt` / `RecordedAt` is non-null and deterministic/recent enough.
- Existing duplicate guard prevents repeated insertion.
- `LastLocationText` returns marina name first and coordinates fallback second.

Expected BFF output:

```json
{
  "latitude": 37.0344,
  "longitude": 27.4305,
  "lastPositionDate": "2026-06-19T10:00:00Z",
  "lastLocationText": "Bodrum Marina"
}
```
