# Inktavia AdminPanel BFF — Postman Collection

## Overview

This Postman collection covers all AdminPanel BFF endpoints for the Inktavia Marine OS platform.

- **Collection:** `Inktavia_AdminPanel_BFF.postman_collection.json`
- **Environment:** `Inktavia_AdminPanel_BFF_Local.postman_environment.json`
- **Schema:** Postman Collection v2.1.0
- **Total Requests:** 55 across 11 folders

---

## Auth Model

AdminPanel BFF uses the **Identity-only browser auth model**.

### For all protected requests (browser / Postman simulation):

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

### What NOT to send to AdminPanel BFF:

```http
Authorization: Bearer <keycloakAccessToken>   ← Do NOT send this
```

The AdminPanel BFF acquires the Keycloak service token **server-side** using `admin-panel-bff` client credentials. This is transparent to the browser and Postman caller.

---

## Quick Start

1. Import both files into Postman:
   - `Inktavia_AdminPanel_BFF.postman_collection.json`
   - `Inktavia_AdminPanel_BFF_Local.postman_environment.json`

2. Select the **Inktavia AdminPanel BFF - Local** environment.

3. Run a login request from **00 - Auth Setup**:
   - `POST Login with Username`
   - `POST Login with Phone`
   - `POST Login with OTP` (requires prior OTP send)

4. The login requests automatically capture `identityAccessToken` and `X_Aizen_User_Token` environment variables.

5. All protected requests will automatically use the captured `X_Aizen_User_Token` value.

---

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `admin_panel_bff_root_url` | `http://localhost:5085` | AdminPanel BFF root URL |
| `admin_panel_bff_base_url` | `{{admin_panel_bff_root_url}}/api/v1/admin-panel` | AdminPanel BFF base path |
| `identityAccessToken` | *(auto-captured)* | Identity JWT access token |
| `identityRefreshToken` | *(auto-captured)* | Identity JWT refresh token |
| `X_Aizen_User_Token` | *(auto-captured)* | Full `Bearer <token>` value |
| `sampleProfileId` | `00000000-0000-0000-0000-000000000000` | Sample profile GUID |
| `sampleOrganizerProfileId` | `00000000-0000-0000-0000-000000000000` | Sample organizer profile GUID |
| `sampleVenueProfileId` | `00000000-0000-0000-0000-000000000000` | Sample venue profile GUID |
| `sampleParticipantProfileId` | `00000000-0000-0000-0000-000000000000` | Sample participant profile GUID |
| `sampleUserId` | `1` | Sample user ID (long) |
| `sampleFileId` | `1` | Sample file ID |
| `sampleVesselId` | `1` | Sample vessel ID |
| `sampleDocumentId` | `1` | Sample document ID |
| `sampleServiceRequestId` | `1` | Sample service request ID |
| `sampleDisputeId` | `1` | Sample dispute ID |
| `sampleCountryId` | `1` | Sample country ID |
| `sampleLookupGroupCode` | `vessel_type` | Sample lookup group code |
| `samplePageIndex` | `0` | Default page index |
| `samplePageSize` | `20` | Default page size |
| `keycloak_token_url` | `http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token` | Keycloak token endpoint (optional folder only) |
| `admin_panel_bff_client_id` | `admin-panel-bff` | BFF Keycloak client ID (optional folder only) |
| `admin_panel_bff_client_secret` | `local-dev-only-change-me` | BFF Keycloak client secret (optional folder only, never commit real value) |
| `adminPanelBffServiceToken` | *(auto-captured in optional folder)* | BFF Keycloak service token — NOT used in normal BFF requests |

---

## Folder Structure

| Folder | Requests | Auth | Description |
|--------|----------|------|-------------|
| 00 - Auth Setup | 7 | Anonymous / Protected | Identity login, OTP, refresh, password change |
| 00 - Keycloak BFF Service Token Validation (Optional) | 1 | None | Optional: validate BFF client_credentials token only |
| 01 - Dashboard | 1 | Protected | Dashboard overview aggregation |
| 02 - Identity - General Profiles | 3 | Protected | General profile list, detail, with-roles |
| 03 - Identity - Organizer Profiles | 5 | Protected | Organizer list, detail, with-user, approve, reject |
| 04 - Identity - Venue Profiles | 4 | Protected | Venue list, detail, approve, reject |
| 05 - Identity - Participant Profiles | 2 | Protected | Participant list and detail |
| 06 - Files | 5 | Protected | File metadata, read URLs, delete, visibility |
| 07 - Vessels | 9 | Protected | Vessel list, detail, update, archive, restore, status, document removal |
| 08 - Service Requests | 10 | Protected | Service request list, disputes, cancel, completion, dispute management |
| 09 - Reference Data | 8 | Protected | Lookups, currencies, locations, measurement units, system parameters |

---

## Keycloak Validation Folder (Optional)

The **00 - Keycloak BFF Service Token Validation (Optional)** folder is provided for development and debugging purposes only.

It allows you to:
- Obtain the `admin-panel-bff` Keycloak `client_credentials` token directly
- Inspect the token claims (decode at jwt.io)

**This token MUST NOT be used as the Authorization header for normal AdminPanel BFF requests.**

The AdminPanel BFF handles this token entirely server-side.

---

## Token Capture Scripts

Login requests (`Login with Username`, `Login with Phone`, `Login with OTP`, `Refresh Token`) automatically capture:

```javascript
const json = pm.response.json();
const data = json.data || json;
const token = data.token || data;
const accessToken = token.accessToken || data.accessToken || data.identityAccessToken;
const refreshToken = token.refreshToken || data.refreshToken || data.identityRefreshToken;
if (accessToken) {
  pm.environment.set('identityAccessToken', accessToken);
  pm.environment.set('X_Aizen_User_Token', `Bearer ${accessToken}`);
}
if (refreshToken) {
  pm.environment.set('identityRefreshToken', refreshToken);
}
```

---

## Local Service Ports

| Service | Port |
|---------|------|
| AdminPanel BFF | 5085 (http) / 7004 (https) |
| Keycloak | 8080 |
| Identity API | 7101 |
| ReferenceData API | 7104 |
| Vessel API | 7105 |
| FileStorage API | 7106 |
| ServiceRequest API | 7107 |

---

## Notes

- All enum fields (e.g., `reason` in Archive Vessel, `status` in Update Dispute Status) accept integer values. Check the respective Abstraction enum files for valid values.
- Sample GUID values (`sampleProfileId`, etc.) are placeholders. Replace them with real IDs from your local database.
- The `admin_panel_bff_client_secret` variable uses a local-dev placeholder. Never commit a real secret.
