# 03 — Fix Operational Status and Asset Type Fields

## Problem

The live response contains `status` and `statusLabel`, but does not contain:

- `operationalStatus`
- `operationalStatusLabel`
- `assetType`
- `assetTypeLabel`

## Expected Response Fields

```json
{
  "operationalStatus": 1,
  "operationalStatusLabel": "In Service",
  "assetType": 1,
  "assetTypeLabel": "Motor Yacht",
  "status": 2,
  "statusLabel": "Active"
}
```

## Files to Audit and Fix

```text
Modules/Vessel/src/Aizen.Modules.Vessel.Abstraction/Dto/Vessel/VesselListItemDto.cs
Modules/Vessel/src/Aizen.Modules.Vessel.Application/Query/Vessel/GetAllVesselsAdmin/GetAllVesselsAdminQueryHandler.cs
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Dto/VesselListItemBffDto.cs
Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/AdminVessels/Query/GetAdminVesselListBffQueryHandler.cs
```

## Required Checks

1. `VesselListItemDto` must expose:
   - `int? OperationalStatus`
   - `int? AssetType`
2. `GetAllVesselsAdminQueryHandler` must project:
   - `OperationalStatus = v.OperationalStatus`
   - `AssetType = v.AssetType`
3. `VesselListItemBffDto` must expose:
   - `int? OperationalStatus`
   - `string? OperationalStatusLabel`
   - `int? AssetType`
   - `string? AssetTypeLabel`
4. BFF handler must map numeric values and label values.

## Label Dictionaries

```csharp
private static readonly IReadOnlyDictionary<int, string> OperationalStatusLabels = new Dictionary<int, string>
{
    [1] = "In Service",
    [2] = "Refit",
    [3] = "Idle",
    [4] = "Decommissioned"
};

private static readonly IReadOnlyDictionary<int, string> AssetTypeLabels = new Dictionary<int, string>
{
    [1] = "Motor Yacht",
    [2] = "Sailing Yacht",
    [3] = "Superyacht",
    [4] = "Catamaran",
    [5] = "RIB",
    [6] = "Commercial"
};
```

## Seed Data Correction

If existing mock vessel rows have `OperationalStatus` or `AssetType` as null, update local/dev mock data idempotently.

Suggested mapping:

```text
20001 Blue Octopus: AssetType=1, OperationalStatus=1
20002 Golden Tide: AssetType=2, OperationalStatus=1
20003 Marmara Pearl: AssetType=1, OperationalStatus=1
20004 Aegean Wind: AssetType=2, OperationalStatus=3
20005 CargoDry Demo Boat: AssetType=1, OperationalStatus=1
20006 Kalamış Runner: AssetType=1, OperationalStatus=2
20007 Bodrum Star: AssetType=6, OperationalStatus=1
20008 Göcek Breeze: AssetType=2, OperationalStatus=1
```

Use existing domain methods if available. If no domain method exists, add a proper domain method rather than reflection or private setter hacks.
