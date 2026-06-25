# Vessel Module Application Query & Command Report

## Scope
Changes made to the Vessel module Application layer as part of the Vessel UI BFF Contract implementation.

## Files Changed

### Query Extensions

#### GetAllVesselsAdminQuery
- File: `Modules/Vessel/src/Aizen.Modules.Vessel.Application/Query/Vessel/GetAllVesselsAdmin/GetAllVesselsAdminQuery.cs`
- Added: `AssetTypes` (`int[]?`), `OwnershipStatuses` (`int[]?`), `OperationalStatuses` (`int[]?`) filter properties
- Constructor updated to accept new nullable filter arrays

#### GetAllVesselsAdminQueryHandler
- File: `Modules/Vessel/src/Aizen.Modules.Vessel.Application/Query/Vessel/GetAllVesselsAdmin/GetAllVesselsAdminQueryHandler.cs`
- Updated predicate to filter on `AssetType` and `OperationalStatus` when filters are provided
- Updated selector to project new fields: `OperationalStatus`, `AssetType`, `LengthMeters`, `GrossTonnage`, `Latitude`, `Longitude`, `LastPositionDate`
- Note: `OwnershipStatuses` filter requires owner join which may not be supported via `GetPagedListAsync` predicate directly; documented as a limitation

### Mapping Extensions

#### VesselMappingExtensions
- File: `Modules/Vessel/src/Aizen.Modules.Vessel.Application/Mapping/VesselMappingExtensions.cs`
- Updated `ToListItemDto`: added `OperationalStatus`, `AssetType`, `OwnershipStatus`, `OwnerName`, `LengthMeters`, `GrossTonnage`, `Latitude`, `Longitude`, `LastPositionDate`
- Updated `ToDto` for `VesselDocumentEntity`: added `DocumentCategory`, `IssuingAuthority`, `ApprovedAt`, `ApprovedByUserId`
- Updated `ToDto` for `VesselMediaEntity`: added `Title`, `Description`, `ThumbnailUrl`, `UploadedByUserId`
- Updated `ToDto` for `VesselEngineEntity`: added `PropulsionType`, `EnginePowerKw`, `FuelCapacityL`, `MaxSpeedKnots`, `CruisingSpeedKnots`, `RangeNm`
- Updated `ToDto` for `VesselSpecificationEntity`: added `BuildCountry`, `SuperstructureMaterial`, `GrossTonnage`, `NetTonnage`, `PassengerCapacity`, `CrewCapacity`

### Vessel Admin Controller
- File: `Modules/Vessel/src/Aizen.Modules.Vessel/Controller/V1/Admin/Vessel/VesselAdminController.cs`
- Added query parameters: `assetTypes`, `ownershipStatuses`, `operationalStatuses`
- Passed to `GetAllVesselsAdminQuery`

## Architecture Compliance

- CQRS pattern maintained (Query and Handler in separate files)
- `IAizenQueryHandlerCacheable` interface retained on `GetAllVesselsAdminQueryHandler`
- All new fields are additive (nullable) — no breaking changes to existing queries
- `DocumentationInfo` attribute present on all modified classes

## Known Limitations

| Limitation | Impact | Workaround |
|-----------|--------|------------|
| `OwnershipStatuses` filter not applied in query predicate | Cannot filter list by ownership status server-side | Post-MVP: add owner join in query |
| `OwnerName` requires Identity cross-join | Returns `null` in list items | Post-MVP: BFF identity enrichment |
| `LastPositionDate` / `Latitude` / `Longitude` require location snapshot include | Only populated when vessel loaded with navigation properties | Ensure include is used in handler |
