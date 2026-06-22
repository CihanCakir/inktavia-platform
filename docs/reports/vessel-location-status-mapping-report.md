# Vessel Location and Status Mapping Report

## Scope
Fix for empty Last Location and Status columns in the Vessel list.

## Files Modified

| File | Change |
|------|--------|
| `Modules/Vessel/src/Aizen.Modules.Vessel.Abstraction/Dto/Vessel/VesselListItemDto.cs` | Added `OwnerUserId`, `OwnerProfileId`, `LastLocationMarinaName` |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Application/Query/Vessel/GetAllVesselsAdmin/GetAllVesselsAdminQueryHandler.cs` | Added location snapshot join and owner join in EF projection |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Dto/VesselListItemBffDto.cs` | Added label fields and `LastLocationText` |
| `Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Query/GetAdminVesselListBffQueryHandler.cs` | Added static label mapping and `LastLocationText` computation |

## Location Mapping

### Vessel Module (EF Projection)
The `GetAllVesselsAdminQueryHandler` now subqueries `VesselLocationSnapshots` inline:

```csharp
Latitude = (double?)v.LocationSnapshots
    .Where(l => l.IsCurrent && l.IsActive)
    .OrderByDescending(l => l.CapturedAt)
    .Select(l => l.Latitude)
    .FirstOrDefault(),
Longitude = ... (same pattern)
LastPositionDate = ... (CapturedAt)
LastLocationMarinaName = ... (MarinaName)
```

`VesselLocationSnapshotEntity` uses `decimal?` for `Latitude`/`Longitude` — cast to `double?` in projection for frontend compatibility.

### BFF (LastLocationText)
```text
Priority 1: MarinaName if non-empty
Priority 2: "lat, lon" formatted string (0.#### precision)
Priority 3: null
```

## Status Label Mapping

Hardcoded in BFF `GetAdminVesselListBffQueryHandler` as static dictionaries.

### OperationalStatus
| Value | Label |
|-------|-------|
| 1 | In Service |
| 2 | Refit |
| 3 | Idle |
| 4 | Decommissioned |

### AssetType
| Value | Label |
|-------|-------|
| 1 | Motor Yacht |
| 2 | Sailing Yacht |
| 3 | Superyacht |
| 4 | Catamaran |
| 5 | RIB |
| 6 | Commercial |

### OwnershipStatus
| Value | Label |
|-------|-------|
| 1 | Private |
| 2 | Charter |
| 3 | Corporate |

### VesselStatus (entity enum)
| Value | Label |
|-------|-------|
| 1 | Draft |
| 2 | Active |
| 3 | Passive |
| 4 | Under Maintenance |
| 5 | Sold |
| 6 | Archived |

## New BFF DTO Fields
```csharp
public string? OperationalStatusLabel { get; set; }
public string? AssetTypeLabel { get; set; }
public string? OwnershipStatusLabel { get; set; }
public string? StatusLabel { get; set; }
public string? LastLocationText { get; set; }
```
All additive — existing numeric fields preserved for filter use.

## Build Validation
```
dotnet build Aizen.sln --no-incremental
→ 0 Error(s)
```

## Remaining Gaps
- EF projection uses inline subquery on `LocationSnapshots` navigation property. If EF cannot translate this to SQL efficiently, consider a separate join in the query. Verify with EF query logs at runtime.
- `ReferenceData` does not own AssetType/OperationalStatus/OwnershipStatus yet — static mapping in BFF is the correct interim approach.
