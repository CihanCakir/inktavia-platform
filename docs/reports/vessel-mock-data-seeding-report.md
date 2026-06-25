# Vessel Mock Data Seeding Report

**Date**: 2026-06-14  
**Module**: Vessel (`Aizen.Modules.Vessel`)

---

## 1. Scope

Creates 8 demo vessels with matching owners, specifications, and engines. Vessels are owned by the stable Identity user IDs seeded by the Identity mock data seeder.

---

## 2. Files Created / Modified

| File | Action | Description |
|------|--------|-------------|
| `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/MockData/VesselMockDataSeeder.cs` | Created | Main seeder class |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/MockData/MockDataSeedOptions.cs` | Created | Options class |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessels.json` | Created | 8 vessel definitions |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-owners.json` | Created | 8 owner records |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-specifications.json` | Created | 8 spec records |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/Seed/Json/MockData/admin-demo/vessel-engines.json` | Created | 8 engine records |
| `Modules/Vessel/src/Aizen.Modules.Vessel.Repository/DependencyInjection.cs` | Modified | `AddVesselMockData()`, calls seeder in `SeedVesselAsync()` |
| `Modules/Vessel/src/Aizen.Modules.Vessel/Program.cs` | Modified | Calls `AddVesselMockData()` |
| `Modules/Vessel/src/Aizen.Modules.Vessel/configuration/appsettings.json` | Modified | Added `MockData` section (disabled) |
| `Modules/Vessel/src/Aizen.Modules.Vessel/configuration/appsettings.Local.json` | Modified | Added `MockData` (enabled) |

---

## 3. Architecture Decisions

### 3.1 Factory Methods + Explicit ID Override
`VesselEntity`, `VesselOwnerEntity`, etc. all have private setters. The seeder:
1. Calls the entity factory method (e.g. `VesselEntity.Create(...)`) to produce a valid entity
2. Sets `entity.Id = stableId` immediately (possible because `AizenEntity.Id` has a public setter)
3. `EF Core` tracks the entity with the explicit ID value

### 3.2 JSON File Discovery
JSON files are discovered relative to `AppContext.BaseDirectory`:
```
{BaseDirectory}/Seed/Json/MockData/{DataSet}/vessels.json
```
The Repository `.csproj` already contains:
```xml
<Content Include="Seed\Json\**\*" CopyToOutputDirectory="PreserveNewest" />
```
No new `.csproj` changes were needed.

### 3.3 Cross-Module References
`VesselOwnerEntity.UserId` and `UserProfileId` reference stable Identity IDs (10003–10008, 11003–11008). The Vessel module database has no foreign key constraint to Identity tables (separate databases), so these are soft references validated by application logic.

---

## 4. Demo Vessels

| Vessel ID | Owner UserId | Owner ProfileId | Name | Type | Status |
|-----------|-------------|----------------|------|------|--------|
| 20001 | 10003 | 11003 | Blue Octopus | MOTOR_YACHT | Active |
| 20002 | 10004 | 11004 | Golden Tide | SAILING | Active |
| 20003 | 10007 | 11007 | Marmara Pearl | MOTOR_YACHT | Active |
| 20004 | 10008 | 11008 | Aegean Wind | GULET | Passive |
| 20005 | 10003 | 11003 | CargoDry Demo Boat | CARGO_DRY | Active |
| 20006 | 10004 | 11004 | Kalamış Runner | SPEEDBOAT | UnderMaintenance |
| 20007 | 10007 | 11007 | Bodrum Star | MOTOR_YACHT | Active |
| 20008 | 10008 | 11008 | Göcek Breeze | SAILING | Active |

---

## 5. Vessel Specifications (Sample)

Each vessel gets one `VesselSpecificationEntity`:
- Brand: Prestige, Beneteau, Azimut, etc.
- Length range: 9m–22m
- Production years: 2012–2023

---

## 6. Vessel Engines (Sample)

Each vessel gets one primary `VesselEngineEntity`:
- Engine types: Inboard, Outboard
- Fuel types: Diesel, Gasoline
- HP range: 150–800

---

## 7. Idempotency Strategy

```csharp
var exists = await dbContext.Vessels.AnyAsync(v => v.Id == model.Id, ct);
if (!exists)
{
    var vessel = VesselEntity.Create(...);
    vessel.Id = model.Id;
    dbContext.Vessels.Add(vessel);
}
```
Same pattern for all sub-entities.

---

## 8. Environment Guard

Same as Identity module:
- `MockData.Enabled` + `MockData.RunOnStartup` + `EnvironmentGuard`
- Default `appsettings.json` has `Enabled: false`
- `appsettings.Local.json` has `Enabled: true`

---

## 9. Validation Results

- **Build**: ✅ 0 errors
- **JSON files**: ✅ Correct location, copied to output directory
- **DI registration**: ✅ `AddVesselMockData()` registered
- **Seeder called**: ✅ In `SeedVesselAsync()` after migration

---

## 10. Remaining Gaps / Follow-ups

- `VesselDocumentEntity` and `VesselMediaEntity` are not seeded. Documents require FileStorage `FileId` values; media requires uploaded files. These can be added in a future iteration once FileStorage mock data seeding is implemented.
- `VesselLocationSnapshotEntity` is not seeded. Can be added as static GPS coordinate fixtures for Bodrum/Marmara marina locations.
- `VesselStatusHistoryEntity` entries are not seeded. Can be added to demonstrate status transition history in the admin panel.
