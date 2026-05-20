# Postman Implementation Tasks

## 1. Create Postman Environment

Create:

```text
infrastructure/postman/inktavia-local.postman_environment.json
```

The environment must contain:

```text
keycloak_base_url
keycloak_realm
keycloak_token_url
identity_api_base_url
profile_api_base_url
payment_api_base_url
mobile_client_id
customer_panel_client_id
admin_panel_client_id
mobile_username
customer_username
admin_username
default_password
identity_api_audience
profile_api_audience
payment_api_audience
mobile_access_token
customer_access_token
admin_access_token
active_access_token
```

## 2. Create Postman Collection

Create:

```text
infrastructure/postman/inktavia-keycloak-api-tests.postman_collection.json
```

Folder structure:

```text
00 - Keycloak Auth
01 - Identity API
02 - Profile API
03 - Payment API
04 - Auto-Discovered Endpoints
90 - Negative Authorization Tests
99 - Diagnostics
```

## 3. Add Token Helper Requests

Add token helper requests:

```text
Get Mobile Token - inktavia-mobile
Get Customer Token - customer-panel
Get Admin Token - admin-panel
```

Each request:

```text
POST {{keycloak_token_url}}
Content-Type: application/x-www-form-urlencoded
```

Body:

```text
grant_type=password
client_id={{mobile_client_id}}
username={{mobile_username}}
password={{default_password}}
```

Equivalent request bodies must exist for customer and admin.

### Important

These requests work only if direct access grant is enabled for local testing.

If direct access grant is disabled, use Postman OAuth2 Authorization Code + PKCE and manually save tokens to:

```text
mobile_access_token
customer_access_token
admin_access_token
```

## 4. Add API Requests

Add representative requests:

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

If actual controllers contain different routes, sync them from controller files.

## 5. Add Negative Tests

Add negative scenarios:

```text
Mobile token cannot access Payment API.
Customer token cannot execute Payment write.
Anonymous request returns 401.
Token with missing role returns 403.
Token with wrong audience returns 401/403.
```

## 6. Add Diagnostics

Add diagnostics requests:

```text
OpenID Configuration
JWKS
Token Endpoint
```

URLs:

```text
GET {{keycloak_base_url}}/realms/{{keycloak_realm}}/.well-known/openid-configuration
GET {{keycloak_base_url}}/realms/{{keycloak_realm}}/protocol/openid-connect/certs
POST {{keycloak_token_url}}
```

## 7. Add Sync Tool

Create:

```text
tools/postman/sync-postman-collection.mjs
```

This tool must:

```text
- Scan src/MDYKE/**/Controller/V1/**/*Controller.cs
- Scan src/MDYKE/**/Controllers/V1/**/*Controller.cs
- Parse controller route attributes
- Parse action route attributes
- Parse HTTP method attributes
- Detect Authorize attributes where possible
- Detect module from path or controller name
- Update infrastructure/postman/inktavia-keycloak-api-tests.postman_collection.json
- Put generated requests under 04 - Auto-Discovered Endpoints
- Preserve manually curated requests
- Avoid duplicate generated requests
```

Preferred command:

```bash
node tools/postman/sync-postman-collection.mjs
```

If `package.json` exists, add:

```json
{
  "scripts": {
    "postman:sync": "node tools/postman/sync-postman-collection.mjs"
  }
}
```

## 8. Documentation

Create:

```text
docs/postman/POSTMAN_LOCAL_TESTING.md
```

It must explain:

```text
- How to import environment
- How to import collection
- How to get tokens
- How to set active token
- How to test each API
- How to run endpoint sync command
- How to handle direct grant disabled
- How to align actual controller routes
```
