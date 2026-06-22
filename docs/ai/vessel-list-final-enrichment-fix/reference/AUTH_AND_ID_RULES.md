# Auth and ID Rules

## ID Rules

This project uses `long` / `long?` for business entity IDs.

Examples:

```text
VesselId: long
OwnerUserId: long?
OwnerProfileId: long?
VesselMediaId: long
VesselDocumentId: long
ServiceRequestId: long
```

Do not introduce `Guid`, `Guid?`, `Guid.Parse(...)`, `Guid.NewGuid()`, or UUID string IDs for business entities.

Exception:

- `FileId` may be `Guid?` for FileStorage integration only if current source code proves it.

## Browser → AdminPanel BFF

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

The browser does not need to provide the internal Keycloak service token.

## AdminPanel BFF → Internal Modules

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <incoming identityAccessToken>
```

The BFF must obtain the Keycloak service token server-side through the existing `IAdminPanelBffKeycloakServiceTokenProvider`.

Do not forward an incoming browser `Authorization` header to internal module APIs.

## Authorization

- AdminPanel BFF endpoints must preserve `AdminPanelAccess` policy.
- Internal admin module endpoints must preserve existing Admin role checks.
- If optional enrichment fails, return warnings and keep vessel rows.
