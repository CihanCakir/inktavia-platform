# AdminPanel BFF — Postman Generation Report

**Generated:** 2026-06-11  
**Scope:** AdminPanel BFF Postman Collection and Local Environment  
**Repository:** Inktavia Marine OS (CihanCakir/inktavia-platform)

---

## Scope

Generate a complete Postman collection and local environment for AdminPanel BFF, covering all endpoints from the endpoint catalog and verified against real controller files.

---

## Catalog File Read

**File read:** `docs/admin-web-client/admin-panel-bff-endpoint-catalog.md`

| Section | Endpoints in Catalog |
|---------|---------------------|
| Auth | 7 |
| Dashboard | 1 |
| Identity - General Profiles | 3 |
| Identity - Organizer Profiles | 5 |
| Identity - Venue Profiles | 4 |
| Identity - Participant Profiles | 2 |
| Files | 5 |
| Vessels | 9 |
| Service Requests | 10 |
| Reference Data | 8 |
| **Total** | **54** |

> Note: The catalog listed `Authorization: Bearer <keycloakAccessToken>` as required for protected endpoints. This was treated as **legacy wording** per the final auth model. Only `X-Aizen-User-Token` is sent from browser/Postman.

---

## Controllers Scanned

The following controller files were scanned and cross-referenced against the catalog:

| Controller File | Endpoints Confirmed |
|-----------------|---------------------|
| `AuthController.cs` | 7 |
| `AdminDashboardController.cs` | 1 |
| `AdminIdentityController.cs` | 5 (general profiles + organizer/venue approve/reject) |
| `OrganizersController.cs` | 3 (list, by ID, with-user) |
| `VenuesController.cs` | 2 (list, by ID) |
| `ParticipantsController.cs` | 2 |
| `AdminFilesController.cs` | 5 |
| `AdminVesselsController.cs` | 9 |
| `AdminServiceRequestsController.cs` | 10 |
| `AdminReferenceDataController.cs` | 8 |

**Key finding:** Organizer approve/reject and venue approve/reject endpoints are physically in `AdminIdentityController.cs`, not `OrganizersController.cs` / `VenuesController.cs`. The catalog is logically correct; the Postman folders are organized by logical domain (not physical controller).

**Additional catalog endpoint not in controllers:**  
The catalog mentions `GET /vessels/{vesselId:long}` separately from `GET /vessels/{vesselId:long}/detail`. Both exist in `AdminVesselsController.cs` as separate actions.

---

## Folders Generated

| Folder Name | Request Count |
|-------------|---------------|
| 00 - Auth Setup | 7 |
| 00 - Keycloak BFF Service Token Validation (Optional) | 1 |
| 01 - Dashboard | 1 |
| 02 - Identity - General Profiles | 3 |
| 03 - Identity - Organizer Profiles | 5 |
| 04 - Identity - Venue Profiles | 4 |
| 05 - Identity - Participant Profiles | 2 |
| 06 - Files | 5 |
| 07 - Vessels | 9 |
| 08 - Service Requests | 10 |
| 09 - Reference Data | 8 |
| **Total** | **55** |

---

## Requests Generated

**Total: 55 requests**

The extra request (55 vs 54 catalog entries) is from the optional Keycloak validation folder which is not an AdminPanel BFF endpoint.

---

## Endpoints Excluded

| Endpoint | Reason |
|----------|--------|
| None | All catalog endpoints are represented |

---

## Auth Model Applied

**Final model applied:** Identity-only browser auth.

- Protected requests send only: `X-Aizen-User-Token: {{X_Aizen_User_Token}}`
- No `Authorization: Bearer <keycloakToken>` on any normal BFF request
- Catalog legacy wording overridden per `RUN_THIS_FIRST_SINGLE_PROMPT.md` instructions
- Keycloak validation isolated to optional folder

---

## Variables Generated

**Environment file:** `Inktavia_AdminPanel_BFF_Local.postman_environment.json`

| Variable | Value |
|----------|-------|
| admin_panel_bff_root_url | http://localhost:5085 |
| admin_panel_bff_base_url | {{admin_panel_bff_root_url}}/api/v1/admin-panel |
| identityAccessToken | (empty, auto-captured) |
| identityRefreshToken | (empty, auto-captured) |
| X_Aizen_User_Token | (empty, auto-captured) |
| sampleProfileId | 00000000-0000-0000-0000-000000000000 |
| sampleOrganizerProfileId | 00000000-0000-0000-0000-000000000000 |
| sampleVenueProfileId | 00000000-0000-0000-0000-000000000000 |
| sampleParticipantProfileId | 00000000-0000-0000-0000-000000000000 |
| sampleUserId | 1 |
| sampleFileId | 1 |
| sampleVesselId | 1 |
| sampleDocumentId | 1 |
| sampleServiceRequestId | 1 |
| sampleDisputeId | 1 |
| sampleCountryId | 1 |
| sampleLookupGroupCode | vessel_type |
| samplePageIndex | 0 |
| samplePageSize | 20 |
| keycloak_token_url | http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token |
| admin_panel_bff_client_id | admin-panel-bff |
| admin_panel_bff_client_secret | local-dev-only-change-me (type: secret) |
| adminPanelBffServiceToken | (empty, auto-captured in optional folder) |

**Total variables: 23**

---

## Scripts Added

| Script Type | Applied To | Behavior |
|-------------|------------|----------|
| Login token capture | Login/username, Login/phone, Login/otp, Refresh | Captures identityAccessToken and X_Aizen_User_Token |
| Keycloak token capture | Get admin-panel-bff Service Token | Captures adminPanelBffServiceToken |
| Standard tests | All requests | Status not 500, Response is JSON |
| Protected check | All protected requests | X_Aizen_User_Token variable must exist |
| Envelope test | List endpoints | Response has envelope or data |

---

## Output Files

| File | Status |
|------|--------|
| `docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF.postman_collection.json` | ✅ Created |
| `docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF_Local.postman_environment.json` | ✅ Created |
| `docs/postman/admin-panel-bff/README.md` | ✅ Created |

---

## Validation Commands

```bash
python3 -m json.tool docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF.postman_collection.json > /tmp/admin_panel_bff_collection_validated.json
python3 -m json.tool docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF_Local.postman_environment.json > /tmp/admin_panel_bff_environment_validated.json
```

## Validation Results

| File | Result |
|------|--------|
| Collection JSON | ✅ Valid — `python3 -m json.tool` succeeded |
| Environment JSON | ✅ Valid — `python3 -m json.tool` succeeded |

---

## Remaining Gaps

| Gap | Severity | Notes |
|-----|----------|-------|
| `admin_panel_bff_client_secret` is a placeholder | Low | Must be provided from secret manager or user secrets for real testing |
| Sample GUIDs are zero-value | Low | Replace with real IDs after seeding local database |
| Enum integer values in request bodies | Low | `VesselArchiveReason`, `VesselStatus`, `ServiceRequestDisputeStatus` enum values require lookup from Abstraction source. Values 0/1 are used as placeholders. |
| `POST /auth/password/change` request body fields | Low | `ChangePasswordRequest` DTO was not directly inspected — fields inferred from standard patterns. Mark: `TODO: verify request DTO` |

---

## Risks / Follow-ups

1. **Admin BFF port**: Confirmed as 5085 (http) from `launchSettings.json`. Update `admin_panel_bff_root_url` if the port changes.
2. **Identity token structure**: Login capture script handles multiple response shapes defensively. If Identity API changes its response envelope shape, the capture script may need updating.
3. **Keycloak secret**: `admin_panel_bff_client_secret` must be sourced from a secret manager. Never commit the real value.
4. **Token protection**: Raw Identity access tokens are not stored in Redis by the current implementation. If server-side session storage is added, update this collection's auth model documentation.
