# ServiceRequest Field Completeness Rules

Vessel cross-module integration identified that ServiceRequest list data is insufficient for Vessel Detail service history.

## Required fields for list/history DTO

The ServiceRequest admin list/history DTO should expose as many of these as the domain supports:

- `Id`
- `RequestCode`
- `VesselId`
- `VesselName`
- `OwnerUserId`
- `OwnerName`
- `ProviderId`
- `ProviderName`
- `ServiceType`
- `CategoryName`
- `Status`
- `Priority`
- `Location`
- `Notes`
- `CreatedAt`
- `RequestedDate`
- `LastActivityAt`

## Mapping rules

- `date` in Vessel Detail `serviceHistory[]` should prefer completion/resolution date if available; otherwise use `CreateDate`.
- `serviceType` should be a display-safe string. Use existing lookup/name if available; otherwise use enum/code string.
- `provider` should be provider profile/company display name if available; otherwise null.
- `location` should prefer marina/location display name; otherwise use free-text location fields if available.
- `notes` should prefer completion notes or request notes depending on existing ServiceRequest model.
- `status` should be normalized to lower-case UI string unless existing BFF convention uses numeric enum.

## Avoid breaking existing consumers

Add fields to DTOs; do not remove or rename existing fields.
