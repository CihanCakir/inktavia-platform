# Postman Collection Specification

## Required File

Create:

```text
infrastructure/postman/inktavia-keycloak-api-tests.postman_collection.json
```

The file must be importable by Postman.

Use Postman Collection v2.1 schema:

```text
https://schema.getpostman.com/json/collection/v2.1.0/collection.json
```

## Collection Name

```text
Inktavia Keycloak API Tests
```

## Required Folders

```text
00 - Keycloak Auth
01 - Identity API
02 - Profile API
03 - Payment API
04 - Auto-Discovered Endpoints
90 - Negative Authorization Tests
99 - Diagnostics
```

## Auth Strategy

Use explicit Bearer token headers in each request.

Example:

```text
Authorization: Bearer {{admin_access_token}}
```

Do not rely only on collection-level auth because each request may need a different token.

## Keycloak Auth Folder

Required requests:

```text
Get Mobile Token - inktavia-mobile
Get Customer Token - customer-panel
Get Admin Token - admin-panel
Set Active Token - Mobile
Set Active Token - Customer
Set Active Token - Admin
```

### Token Request Body

```text
grant_type=password
client_id={{mobile_client_id}}
username={{mobile_username}}
password={{default_password}}
```

### Token Response Test Script

Each token request must save the access token.

Example:

```javascript
const response = pm.response.json();

pm.test("Token response contains access_token", function () {
  pm.expect(response.access_token).to.be.a("string");
});

pm.environment.set("mobile_access_token", response.access_token);
pm.environment.set("active_access_token", response.access_token);
```

Use the correct variable for each token request.

## Initial API Folders

Identity:

```text
GET {{identity_api_base_url}}/me
GET {{identity_api_base_url}}/admin-only
```

Profile:

```text
GET {{profile_api_base_url}}/profiles/me
PUT {{profile_api_base_url}}/profiles/me
```

Payment:

```text
GET {{payment_api_base_url}}/payments
POST {{payment_api_base_url}}/payments
```

## Auto-Discovered Endpoints Folder

The sync command must place generated requests here.

Request names should follow:

```text
[METHOD] /resolved/route - ControllerName.ActionName
```

Generated request description must contain:

```text
Generated from Controller
Source: relative/path/to/Controller.cs
Action: ActionName
Controller: ControllerName
Detected Policy: PolicyName or Unknown
Detected Module: Identity/Profile/Payment/Unknown
```

## Negative Authorization Tests

Required examples:

```text
Mobile Token -> Payment GET Should Fail
Customer Token -> Payment POST Should Fail
Anonymous -> Identity Me Should Fail
Customer Token -> Admin Only Should Fail
```

Test scripts should allow either 401 or 403 depending on whether the failure is audience or role based.

```javascript
pm.test("Request should be unauthorized or forbidden", function () {
  pm.expect([401, 403]).to.include(pm.response.code);
});
```

## Required Generated Marker

All generated endpoint requests must include one of:

```text
Generated from Controller
x-generated-by: tools/postman/sync-postman-collection.mjs
```

so the sync tool can identify generated requests.
