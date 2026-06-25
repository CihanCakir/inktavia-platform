# CQRS and Application Revision Rules

## Vessel list

Extend the existing admin vessel list query/handler instead of creating duplicate list flows.

Required filters:

- AssetTypes
- OwnershipStatuses
- OperationalStatuses
- Search

Required projection additions where data exists:

- ThumbnailUrl
- OwnerName
- OwnerAvatarUrl if Identity/User profile join is available
- LengthMeters
- GrossTonnage
- Latitude
- Longitude
- LastPositionDate
- OperationalStatus
- AssetType
- OwnershipStatus

## Vessel detail

The BFF detail response must aggregate:

- Vessel base fields
- Vessel specification fields
- Primary media hero image
- Latest location snapshot
- Engine data
- Document summary top 5
- CargoDry kits when available
- Service history when available

If CargoDry/ServiceRequest contracts are unavailable, return empty arrays and report the missing contract.

## Documents

Document list must include versions. Compute in BFF or module application layer:

- daysUntilExpiry
- documentStatus: archived, expired, expiring, valid
- versions[].isCurrent

## Media

Media list must support optional mediaType filter and sort by SortOrder ascending, then CreateDate descending if needed.

## Commands

Approve and replace document commands are Post-MVP unless the UI needs them immediately. If implemented:

- Approve sets ApprovedAt and ApprovedByUserId.
- Replace creates a new VesselDocumentVersion and resets approval.
- Use FileStorage patterns if file upload is required.
