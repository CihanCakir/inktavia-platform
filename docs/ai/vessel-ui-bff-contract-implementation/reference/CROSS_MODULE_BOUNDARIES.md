# Cross-Module Boundaries

## Vessel Module owns

- Vessel
- Specification
- Ownership snapshot/status
- Engine
- Documents and versions
- Media
- Location snapshot

## CargoDry Module owns

- Kit activation data
- expiry/efficiency input data

BFF can query by VesselId if the module supports it. Do not write CargoDry data from Vessel module.

## ServiceRequest Module owns

- Service requests
- Work logs/history
- provider/location/status fields

BFF can query history by VesselId if available. Do not write ServiceRequest data from Vessel module.

## Identity Module owns

- User/profile names
- avatars
- admin user id/name

If ownerAvatarUrl/uploadedByName/approvedByName is unavailable, return null and document the missing profile lookup. Do not join across databases directly unless an approved remote call exists.
