# Postman Local Testing Guide

## 1. Files

Import these files into Postman:

```text
infrastructure/postman/inktavia-local.postman_environment.json
infrastructure/postman/inktavia-keycloak-api-tests.postman_collection.json
```

## 2. Select Environment

Select:

```text
Inktavia Local
```

## 3. Start Local Services

```bash
docker compose up -d
```

Validate Keycloak:

```text
http://localhost:8080
```

Expected realm:

```text
inktavia-realm
```

## 4. Token Flow

Collection folder:

```text
00 - Keycloak Auth
```

Run one of:

```text
Get Mobile Token - inktavia-mobile
Get Customer Token - customer-panel
Get Admin Token - admin-panel
```

These requests save tokens into:

```text
mobile_access_token
customer_access_token
admin_access_token
active_access_token
```

## 5. Important Direct Grant Note

The local token helper requests use:

```text
grant_type=password
```

This only works if direct access grant is enabled for local testing.

For production-like testing, use Postman OAuth2 Authorization Code Flow + PKCE and store the received token manually into the proper token variable.

## 6. Test API Requests

Use folders:

```text
01 - Identity API
02 - Profile API
03 - Payment API
04 - Auto-Discovered Endpoints
```

## 7. Sync New Controller Endpoints

When new endpoints are added under:

```text
Modules/**/Controller/V1/**/*Controller.cs
Modules/**/Controllers/V1/**/*Controller.cs
src/MDYKE/**/Controller/V1/**/*Controller.cs  (spec path, if used)
```

Run:

```bash
node tools/postman/sync-postman-collection.mjs
```

The sync tool scans both `Modules/` (actual project layout) and `src/MDYKE/` (spec path) automatically.

If package.json script exists:

```bash
npm run postman:sync
```

Then re-import or refresh the collection in Postman.

## 8. Negative Tests

Use folder:

```text
90 - Negative Authorization Tests
```

## 9. Diagnostics

Use folder:

```text
99 - Diagnostics
```

## 10. Troubleshooting

### Token request returns unauthorized_client

The client may not allow direct access grant.

Use Authorization Code Flow + PKCE or enable direct access grant only for local testing.

### API returns 401

Check:

```text
- Access token exists.
- Token is not expired.
- API audience exists in aud claim.
- API uses correct KEYCLOAK_AUTHORITY and KEYCLOAK_METADATA_ADDRESS.
```

### API returns 403

Check:

```text
- User has required realm role.
- API maps realm_access.roles to ClaimTypes.Role.
- Correct policy is used on endpoint.
```
