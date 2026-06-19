# Vessel UI Contract Audit Report

## Scope
Vessel Management UI backend contract implementation for AdminPanel BFF — 4 MVP GET endpoints.

## Endpoints Implemented

| Endpoint | Method | Returns | Status |
|---|---|---|---|
| `/api/v1/admin-panel/vessels` | GET | `AdminVesselListBffResponse` | ✅ |
| `/api/v1/admin-panel/vessels/{vesselId}/detail` | GET | `AdminVesselDetailBffResponse` | ✅ |
| `/api/v1/admin-panel/vessels/{vesselId}/documents` | GET | `AdminVesselDocumentsBffResponse` | ✅ NEW |
| `/api/v1/admin-panel/vessels/{vesselId}/media` | GET | `AdminVesselMediaBffResponse` | ✅ |

## Build Status
**Build: SUCCEEDED — 0 errors**

## Architecture Decisions
- All 4 endpoints are additive; no existing endpoints removed
- New BFF queries/handlers created alongside existing ones
- Existing `GetVessels` and `GetVesselDetail` endpoints now return UI-ready flat BFF DTOs
- `GetVesselById` passthrough endpoint retained unchanged
- Service history aggregated from ServiceRequest module in detail handler
- CargoDry integration left as empty list (module not active)

## Remaining Gaps
- `OwnerName` field requires Identity module join — left as `null`
- `OperationalStatus` on `VesselDto` not yet propagated through `VesselDetailDto` (only on `VesselEntity`)
- FileStorage `HeroImageUrl` not yet integrated into detail response
- `UploadedAt` on media not yet stored on `VesselMediaEntity`
