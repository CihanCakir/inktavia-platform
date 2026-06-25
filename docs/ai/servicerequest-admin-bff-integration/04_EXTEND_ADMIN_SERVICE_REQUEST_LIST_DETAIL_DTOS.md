# 04 — Extend Admin ServiceRequest List and Detail DTOs

Add UI-needed fields to ServiceRequest admin list/detail responses.

## List DTO must support

- `Id`
- `RequestCode`
- `VesselId`
- `VesselName`
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

## Detail DTO must support

- base request fields
- vessel summary
- owner/provider summary
- timeline/status history
- offers
- assignments
- work logs
- attachments
- completion summary
- dispute summary

## Rule

If a sub-domain already has a query/DTO, reuse it. If not, add a read-only DTO/projection. Do not implement new write domain behavior here.

## Output

Create/update ServiceRequest abstraction DTOs and application mappers.
