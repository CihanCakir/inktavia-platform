# Appsettings, Environment and Secret Model

## Suggested configuration

```json
{
  "KeycloakServiceToken": {
    "Authority": "http://localhost:8080/realms/inktavia-realm",
    "TokenEndpoint": "http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token",
    "ClientId": "admin-panel-bff",
    "ClientSecret": "__FROM_SECRET__",
    "CacheSecondsBeforeExpiry": 60,
    "CacheKeyPrefix": "inktavia:admin-panel-bff:keycloak-service-token"
  },
  "InternalServices": {
    "IdentityApi": "http://localhost:7101/api/v1",
    "ReferenceDataApi": "http://localhost:7104/api/v1",
    "VesselApi": "http://localhost:7105/api/v1",
    "FileStorageApi": "http://localhost:7106/api/v1",
    "ServiceRequestApi": "http://localhost:7107/api/v1"
  }
}
```

## Secret handling

Do not commit real secrets.

Use:

```text
Environment variables
User secrets
Kubernetes secrets
Secret manager
```

Example:

```text
KeycloakServiceToken__ClientSecret
```

## Validation

Validate required configuration at startup where the repository convention supports it.

Do not log secrets.

If a required secret is missing, fail clearly in development or report a configuration error according to the existing BFF convention.
