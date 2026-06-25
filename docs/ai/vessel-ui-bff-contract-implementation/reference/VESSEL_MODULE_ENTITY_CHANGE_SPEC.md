# Vessel Module Entity Change Spec

Audit before adding anything. Add only fields that are missing in the real entity model.

## Vessel

Add if missing:

- `OperationalStatus` int, default `1`.
- `AssetType` int, nullable or default based on existing pattern.
- `HomePort` string? if not already present.

Prefer enums if the module already uses enum patterns. Otherwise use int-compatible properties and document values.

## VesselSpecification / VesselSpec

Add if missing:

- `BuilderName` string?
- `BuildCountry` string?
- `HullMaterial` string?
- `SuperstructureMaterial` string?

## VesselOwnership

Add if missing:

- `OwnershipStatus` int, default `1`.

## VesselDocument

Add if missing:

- `DocumentCategory` string?
- `IssuingAuthority` string?
- `ApprovedAt` DateTime?
- `ApprovedByUserId` Guid? or int? depending Identity UserId type used by the project.

Do not force Guid if the project uses numeric user ids. Reconcile with `IAizenInfoAccessor` and Identity DTOs.

## VesselDocumentVersion

Add if missing:

- `ThumbnailUrl` string?
- `MimeType` string?
- `FileSizeBytes` long?
- `UploadedByUserId` matching project user id type.
- `Notes` string?

## VesselMedia

Add if missing:

- `Title` string?
- `Description` string?
- `ThumbnailUrl` string?
- `MimeType` string?
- `FileSizeBytes` long?
- `UploadedByUserId` matching project user id type.
- `SortOrder` int default `0`.

## VesselEngine

Create only if no equivalent engine entity/table already exists.

Fields:

- Id
- VesselId
- EngineType
- EngineCount
- EnginePowerKw
- EngineModel
- PropulsionType
- FuelType
- FuelCapacityL
- MaxSpeedKnots
- CruisingSpeedKnots
- RangeNm

Use existing domain base class, audit fields, private setters, factory/update pattern and EF configuration style.
