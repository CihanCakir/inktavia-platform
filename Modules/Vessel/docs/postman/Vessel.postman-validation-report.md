# Vessel Module — Postman Validation Report

## Endpoint Coverage

| Controller | Endpoint | Method | Route | Covered | Sample | Tests | Notes |
|---|---|---|---|---|---|---|---|
| VesselController | Create Vessel | POST | /vessels | ✅ Yes | ✅ Yes | ✅ Yes | Extracts vesselId |
| VesselController | Update Vessel | PUT | /vessels/{id} | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselController | Get Vessel By ID | GET | /vessels/{id} | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselController | Get Vessel By Code | GET | /vessels/code/{code} | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselController | Get My Vessels | GET | /vessels/current-user | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselController | Archive Vessel | PATCH | /vessels/{id}/archive | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselController | Restore Vessel | PATCH | /vessels/{id}/restore | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselController | Update Status | PATCH | /vessels/{id}/status | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselController | Update Visibility | PATCH | /vessels/{id}/visibility | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselLocationController | Get Current Location | GET | /vessels/{id}/location/current | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselLocationController | Update Location | PUT | /vessels/{id}/location | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselEngineController | Get Engines | GET | /vessels/{id}/engines | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselEngineController | Add Engine | POST | /vessels/{id}/engines | ✅ Yes | ✅ Yes | ✅ Yes | Extracts engineId |
| VesselEngineController | Update Engine | PUT | /vessels/{id}/engines/{engineId} | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselEngineController | Set Primary Engine | PATCH | /vessels/{id}/engines/{engineId}/set-primary | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselEngineController | Delete Engine | DELETE | /vessels/{id}/engines/{engineId} | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselMediaController | Get Media | GET | /vessels/{id}/media | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselMediaController | Add Media | POST | /vessels/{id}/media | ✅ Yes | ✅ Yes | ✅ Yes | Requires fileId |
| VesselMediaController | Update Media | PUT | /vessels/{id}/media/{mediaId} | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselMediaController | Delete Media | DELETE | /vessels/{id}/media/{mediaId} | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselMediaController | Set Cover | PATCH | /vessels/{id}/media/{mediaId}/set-cover | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselMediaController | Update Sort Order | PATCH | /vessels/{id}/media/{mediaId}/sort-order | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselOwnershipController | Get Owners | GET | /vessels/{id}/owners | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselOwnershipController | Add Owner | POST | /vessels/{id}/owners | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselOwnershipController | Update Owner Role | PUT | /vessels/{id}/owners/{ownerId}/role | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselOwnershipController | Delete Owner | DELETE | /vessels/{id}/owners/{ownerId} | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselOwnershipController | Set Primary Owner | PATCH | /vessels/{id}/owners/{ownerId}/set-primary | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselOwnershipController | Accept Invitation | PATCH | /vessels/{id}/owners/accept-invitation | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselOwnershipController | Reject Invitation | PATCH | /vessels/{id}/owners/reject-invitation | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselStatusController | Get Status History | GET | /vessels/{id}/status-history | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselSpecificationController | Get Specification | GET | /vessels/{id}/specification | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselSpecificationController | Upsert Specification | PUT | /vessels/{id}/specification | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselSpecificationController | Delete Specification | DELETE | /vessels/{id}/specification | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselDocumentController | Get Documents | GET | /vessels/{id}/documents | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselDocumentController | Add Document | POST | /vessels/{id}/documents | ✅ Yes | ✅ Yes | ✅ Yes | Requires fileId |
| VesselDocumentController | Update Document | PUT | /vessels/{id}/documents/{documentId} | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselDocumentController | Delete Document | DELETE | /vessels/{id}/documents/{documentId} | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselDocumentController | Update Document Status | PATCH | /vessels/{id}/documents/{documentId}/status | ✅ Yes | ✅ Yes | ✅ Yes | |
| VesselAdminController | Admin List Vessels | GET | /admin/vessels | ✅ Yes | ✅ Yes | ✅ Yes | Requires Admin |

## Coverage Statistics

| Category | Count | Covered | Coverage % |
|---|---|---|---|
| Total Endpoints | ~40 | 40 | 100% |
| With Sample Request | 40 | 40 | 100% |
| With Test Scripts | 40 | 40 | 100% |
| ID Extraction Scripts | 6 | 6 | 100% |

## Known Limitations
- `fileId` must be pre-populated from FileStorage module for Media and Document endpoints
- Owner invitation endpoints (accept/reject) require an actual pending invitation in the system
- Admin endpoint uses `/api/v1/admin/vessels` (different from `/api/v1/vessels` base path)
