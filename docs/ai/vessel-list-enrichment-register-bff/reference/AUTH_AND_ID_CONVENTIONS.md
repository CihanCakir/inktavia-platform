# Auth and ID Conventions

## ID convention

This project uses `long` / `long?` IDs for business entities:

- VesselId: `long`
- OwnerUserId: `long?`
- OwnerProfileId/UserProfileId: `long?`
- VesselOwnerId: `long`
- ServiceRequestId: `long`
- ProviderProfileId: `long?`

Do not introduce `Guid` IDs unless the current source explicitly proves the property is a FileStorage field such as `FileId`.

## Browser → AdminPanel BFF

Browser sends:

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Do not require or forward browser-provided `Authorization` as the internal service token.

## AdminPanel BFF → internal modules

BFF must acquire its own Keycloak client credentials token via the existing service token provider and call modules with:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Use the existing `IAdminPanelBffKeycloakServiceTokenProvider` pattern.

## Authorization

Use the existing AdminPanel BFF controller authorization convention, expected:

```csharp
[Authorize(Policy = "AdminPanelAccess")]
```

Do not weaken protected endpoints.
