# Status and Location Target

## Operational Status Labels

```csharp
private static readonly IReadOnlyDictionary<int, string> OperationalStatusLabels = new Dictionary<int, string>
{
    [1] = "In Service",
    [2] = "Refit",
    [3] = "Idle",
    [4] = "Decommissioned"
};
```

## Asset Type Labels

```csharp
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

## Seeded Vessel Operational / Asset Values

If local demo vessel records have null values, update them idempotently:

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

## Location Seed Coverage

Add latest/current location snapshots for missing seeded vessels:

```text
26005 → VesselId 20005 → Bodrum Marina → 37.0344, 27.4305
26006 → VesselId 20006 → Kalamış Marina → 40.9693, 29.0523
26007 → VesselId 20007 → Yalıkavak Marina → 37.1067, 27.2844
26008 → VesselId 20008 → Göcek Marina → 36.7530, 28.9390
```

## LastLocationText Rule

Priority:

1. Marina name if available
2. Formatted coordinates if marina name is empty
3. null
