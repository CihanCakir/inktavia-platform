# Current UI Gaps

## Screenshot-observed Vessel list gaps

The Admin Web Vessel list displays vessel rows correctly, but the following columns are empty or rendered as dash (`—`):

- Owner column (`Sahip`)
- Last location column (`Son Konum`)
- Status column (`Durum`)

The list currently contains rows like:

- Göcek Breeze
- Bodrum Star
- Kalamış Runner
- CargoDry Demo Boat
- Aegean Wind
- Marmara Pearl
- Golden Tide
- Blue Octopus

This means the core list endpoint is working and seed data exists, but BFF/module enrichment is incomplete.

## Screenshot-observed Vessel Register gap

React Admin Web opens:

```http
http://localhost:3000/app/vessels/register
```

The page shows:

```text
Gemi verileri yüklenemedi.
```

The network/backend expectation is:

```http
GET /api/v1/admin-panel/vessels/register
```

Current result:

```text
404 Not Found
```

This endpoint must be implemented as a register-page bootstrap/options endpoint. It must not be confused with the vessel detail route.

## Current BFF handler issue

Current list handler maps fields directly from the Vessel module list response:

```csharp
OwnerName = v.OwnerName,
Latitude = v.Latitude,
Longitude = v.Longitude,
LastPositionDate = v.LastPositionDate,
OperationalStatus = v.OperationalStatus,
AssetType = v.AssetType,
OwnershipStatus = v.OwnershipStatus,
Status = (int)v.Status,
```

But if upstream `OwnerName`, latest location, or status fields are null/default, UI remains empty.

Required fix: The handler must become an aggregate/enrichment handler, not just a mapper.
