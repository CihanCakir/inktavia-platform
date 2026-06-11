# RUN THIS FIRST — AdminPanel BFF Postman Collection Generation

You are working inside the Inktavia Marine OS backend repository.

Your task is to generate a professional Postman collection and local Postman environment for AdminPanel BFF.

Use English for all generated files, Postman request names, folder descriptions, scripts, documentation, and reports.

## Mandatory source file

Read this file first:

```text
docs/admin-web-client/admin-panel-bff-endpoint-catalog.md
```

This file contains the AdminPanel BFF controller endpoint list and endpoint descriptions.

Important: if the endpoint catalog still says protected endpoints require both `Authorization` and `X-Aizen-User-Token`, treat that as legacy wording. The final current BFF browser-facing auth model is Identity-only for AdminPanel BFF requests.

## Final auth model to apply

AdminPanel BFF browser/Postman request model:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Do not send this header to AdminPanel BFF normal protected requests:

```http
Authorization: Bearer <keycloakAccessToken>
```

AdminPanel BFF internally obtains the Keycloak service token with the confidential `admin-panel-bff` client and forwards it to internal APIs. This is server-side and must not be simulated as a browser header for normal AdminPanel BFF requests.

## Optional Keycloak validation folder

Create a separate optional folder called:

```text
00 - Keycloak BFF Service Token Validation (Optional)
```

This folder is only for validating the `admin-panel-bff` client_credentials token in Postman. It must not be used as the Authorization token for ordinary AdminPanel BFF requests.

## Required Postman output

Create:

```text
docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF.postman_collection.json
docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF_Local.postman_environment.json
docs/postman/admin-panel-bff/README.md
```

## Required report output

Create:

```text
docs/reports/admin-panel-bff-postman-generation-report.md
docs/reports/admin-panel-bff-postman-endpoint-coverage-report.md
docs/reports/admin-panel-bff-postman-auth-model-report.md
```

## Folder order in Postman collection

The collection must be organized in this order:

```text
00 - Auth Setup
00 - Keycloak BFF Service Token Validation (Optional)
01 - Dashboard
02 - Identity - General Profiles
03 - Identity - Organizer Profiles
04 - Identity - Venue Profiles
05 - Identity - Participant Profiles
06 - Files
07 - Vessels
08 - Service Requests
09 - Reference Data
90 - Health / Diagnostics (only if real endpoints exist)
99 - Deprecated / Future / Excluded (only if needed)
```

Do not invent endpoints. If a folder has no real endpoints, do not create fake requests.

## Required variable model

Environment variables must include at minimum:

```text
admin_panel_bff_root_url
admin_panel_bff_base_url
identityAccessToken
identityRefreshToken
X_Aizen_User_Token
sampleProfileId
sampleOrganizerProfileId
sampleVenueProfileId
sampleParticipantProfileId
sampleUserId
sampleFileId
sampleVesselId
sampleDocumentId
sampleServiceRequestId
sampleDisputeId
sampleCountryId
sampleLookupGroupCode
samplePageIndex
samplePageSize

keycloak_token_url
admin_panel_bff_client_id
admin_panel_bff_client_secret
adminPanelBffServiceToken
```

`adminPanelBffServiceToken` is optional and must only be used in the optional Keycloak validation folder or direct internal API testing notes. It must not be attached to ordinary AdminPanel BFF requests.

## Auth Setup requirements

Create requests for:

```http
POST /auth/login/username
POST /auth/login/phone
POST /auth/login/otp
POST /auth/otp/send
POST /auth/otp/check
POST /auth/refresh
POST /auth/password/change
```

The login and refresh requests must capture Identity tokens from the response if the response shape contains them.

Support common response shapes defensively:

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

For protected AdminPanel BFF requests, add only:

```http
X-Aizen-User-Token: {{X_Aizen_User_Token}}
```

Do not add Authorization headers to normal AdminPanel BFF requests.

## Request body rules

Derive body examples from:

1. `docs/admin-web-client/admin-panel-bff-endpoint-catalog.md`
2. Real controller action parameters
3. Real request DTOs under BFF/Application or active module Abstraction projects
4. Existing generated API client contracts if available

Do not invent complex DTOs. If a request body cannot be confidently derived, create a minimal placeholder and mark it clearly with `TODO: verify request DTO` in the request body and report.

## Validation rules

Every request must include basic Postman tests:

```javascript
pm.test('Status code is not 500', function () {
  pm.expect(pm.response.code).to.not.equal(500);
});

pm.test('Response is JSON when body exists', function () {
  if (pm.response.text()) {
    pm.expect(() => pm.response.json()).to.not.throw();
  }
});
```

For list endpoints, add tests that allow Aizen envelopes:

```javascript
const json = pm.response.json();
pm.test('Response has recognizable envelope or data', function () {
  pm.expect(json).to.be.an('object');
  pm.expect(json.data !== undefined || json.success !== undefined || Array.isArray(json)).to.equal(true);
});
```

## Validation commands

After generating JSON files, validate they are parseable:

```bash
python -m json.tool docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF.postman_collection.json > /tmp/admin_panel_bff_collection_validated.json
python -m json.tool docs/postman/admin-panel-bff/Inktavia_AdminPanel_BFF_Local.postman_environment.json > /tmp/admin_panel_bff_environment_validated.json
```

Do not fake success. Document blockers clearly.
