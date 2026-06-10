# AdminPanel BFF Configuration and Secret Handling Report

## Report Date
2026-06-10

---

## New Configuration Section

Added to `Bff/src/AdminPanel/Aizen.Bff.AdminPanel/configuration/appsettings.json`:

```json
"KeycloakServiceToken": {
  "Authority": "http://localhost:8080/realms/inktavia-realm",
  "TokenEndpoint": "http://localhost:8080/realms/inktavia-realm/protocol/openid-connect/token",
  "ClientId": "admin-panel-bff",
  "ClientSecret": "__FROM_SECRET__",
  "CacheSecondsBeforeExpiry": 60,
  "CacheKeyPrefix": "inktavia:admin-panel-bff:keycloak-service-token"
}
```

---

## Options Class

`Bff/src/AdminPanel/Aizen.Bff.AdminPanel.Application/Common/Options/KeycloakServiceTokenOptions.cs`

Bound via:
```csharp
services.Configure<KeycloakServiceTokenOptions>(configuration.GetSection(KeycloakServiceTokenOptions.SectionName));
// SectionName = "KeycloakServiceToken"
```

---

## Secret Handling

| Secret | Handling |
|---|---|
| `ClientSecret` | Set to `"__FROM_SECRET__"` in appsettings — must be overridden at runtime |
| Environment variable | `KeycloakServiceToken__ClientSecret` |
| Kubernetes | Inject via Secret → environment variable |
| User secrets (dev) | `dotnet user-secrets set "KeycloakServiceToken:ClientSecret" "<value>"` |

**No real secrets are committed to the repository.**

---

## Startup Validation

The Aizen framework uses `IOptions<T>` injection. If `ClientSecret` is missing or set to the placeholder value at startup, the application will still start but the first Keycloak token request will fail with an authentication error from Keycloak.

**Follow-up:** Add explicit startup validation using `IStartupFilter` or `ValidateOnStart()`:
```csharp
services.AddOptions<KeycloakServiceTokenOptions>()
    .BindConfiguration(KeycloakServiceTokenOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

This requires adding `[Required]` annotations to `Authority`, `TokenEndpoint`, `ClientId`, and `ClientSecret` fields.

---

## Internal Service Base URLs

Existing `RemoteCalls` section in `appsettings.json` provides base URLs for all internal services:

```json
"RemoteCalls": {
  "IIdentityAdminBffRemoteCall": { "BaseUrl": "http://localhost:7101" },
  "IVesselAdminBffRemoteCall": { "BaseUrl": "http://localhost:7105" },
  "IFileStorageAdminBffRemoteCall": { "BaseUrl": "http://localhost:7106" },
  "IServiceRequestAdminBffRemoteCall": { "BaseUrl": "http://localhost:7107" },
  "IReferenceDataAdminBffRemoteCall": { "BaseUrl": "http://localhost:7104" }
}
```

These are not secrets and are correctly stored in `appsettings.json`.
