# 04 — Fix Latest Location and Status Mapping

## Latest location

Audit Vessel module entity/query support:

```bash
grep -r "LocationSnapshot\|Latitude\|Longitude\|RecordedAt\|Marina" Modules/Vessel/src --include="*.cs" -n | head -200
```

Update the Vessel module admin list query/projection so every list item can include the latest location snapshot:

- `Latitude`
- `Longitude`
- `LastPositionDate`
- `LocationMarinaName` or equivalent, if present
- `LastLocationText` if available or BFF-computed

Preferred EF projection pattern:

```csharp
var latestLocation = db.VesselLocationSnapshots
    .Where(l => l.VesselId == v.Id)
    .OrderByDescending(l => l.RecordedAt)
    .Select(l => new { l.Latitude, l.Longitude, l.RecordedAt, l.LocationMarinaName })
    .FirstOrDefault();
```

Adapt to repository/query style. Avoid loading all snapshots into memory.

## Status mapping

Ensure both raw numeric values and UI labels exist.

Additive BFF fields if missing:

```csharp
public string? OperationalStatusLabel { get; set; }
public string? AssetTypeLabel { get; set; }
public string? OwnershipStatusLabel { get; set; }
public string? StatusLabel { get; set; }
public string? LastLocationText { get; set; }
```

Mapping rules:

```text
OperationalStatus:
1 = In Service
2 = Refit
3 = Idle
4 = Decommissioned

AssetType:
1 = Motor Yacht
2 = Sailing Yacht
3 = Superyacht
4 = Catamaran
5 = RIB
6 = Commercial

OwnershipStatus:
1 = Private
2 = Charter
3 = Corporate
```

If existing enum names differ, use existing enum/source mapping instead.

`LastLocationText` fallback:

1. Marina/location name if present
2. `Latitude.ToString("0.####"), Longitude.ToString("0.####")`
3. `null`

Document in:

```text
docs/reports/vessel-location-status-mapping-report.md
```
