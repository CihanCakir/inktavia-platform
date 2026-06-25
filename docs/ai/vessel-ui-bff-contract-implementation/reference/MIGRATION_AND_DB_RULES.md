# Migration and DB Rules

## Audit first

Do not generate migrations until the real entity and DbContext model are inspected.

## Migration groups

Use existing project naming style. Combine fields if that is the local convention; otherwise use focused migrations:

- AddVesselOperationalAndAssetType
- AddVesselSpecExtensions
- AddVesselOwnershipStatus
- AddVesselDocumentExtensions
- AddVesselDocumentVersionExtensions
- AddVesselMediaExtensions
- AddVesselEngineTable

CargoDry and ServiceRequest migrations are only allowed if their modules are in scope and the field is verified missing:

- AddCargoDryKitVesselId
- AddServiceRequestVesselId

## UTC and defaults

Use UTC values and PostgreSQL-compatible defaults. Do not use local time.

## Backward compatibility

New columns should be nullable or have safe defaults unless domain invariants require otherwise.

## Reporting

Every migration must be listed in the final report with:

- Entity changed
- Property changed
- Column type
- Nullable/default
- Migration name
- Reason from UI contract
