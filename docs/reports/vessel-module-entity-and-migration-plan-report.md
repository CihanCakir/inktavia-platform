# Vessel Module Entity and Migration Plan Report

## Entity Changes Applied

### VesselEntity
Added: `OperationalStatus (int?)`, `AssetType (int?)` with `UpdateOperationalStatus()` / `UpdateAssetType()` methods.

### VesselSpecificationEntity
Added: `BuildCountry`, `SuperstructureMaterial`, `GrossTonnage`, `NetTonnage`, `PassengerCapacity`, `CrewCapacity`.

### VesselDocumentEntity
Added: `DocumentCategory`, `IssuingAuthority`, `ApprovedAt`, `ApprovedByUserId` with `Approve(userId)` method.

### VesselMediaEntity
Added: `Title`, `Description`, `ThumbnailUrl`, `UploadedByUserId`.

### VesselEngineEntity
Added: `PropulsionType`, `EnginePowerKw`, `FuelCapacityL`, `MaxSpeedKnots`, `CruisingSpeedKnots`, `RangeNm`.

## EF Configuration Changes
All new fields configured with appropriate `HasMaxLength` / `HasPrecision` / index where relevant.

## Migration Status
**FAILED** — EF migrations tool requires a startup project with a proper connection string and runtime.
The Repository project cannot be used as its own startup project (`System.Runtime` v8.0 vs v9.0 mismatch).

### To apply migration manually:
```bash
dotnet ef migrations add AddVesselUiContractFields \
  --project Modules/Vessel/src/Aizen.Modules.Vessel.Repository \
  --startup-project Modules/Vessel/src/Aizen.Modules.Vessel \
  --context VesselDbContext
```

## Follow-ups
- Run migration against the Vessel API project as startup project
- Apply migration to dev/staging database
