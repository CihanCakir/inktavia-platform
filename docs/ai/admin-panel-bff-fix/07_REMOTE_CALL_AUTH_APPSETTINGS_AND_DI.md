# 07 - Remote Call, Auth Forwarding, Appsettings and DI

Ensure the AdminPanel BFF uses correct remote service configuration.

## Remote service base URLs

Local defaults:

```text
Identity:       http://localhost:7101/api/v1
ReferenceData: http://localhost:7104/api/v1
Vessel:         http://localhost:7105/api/v1
FileStorage:    http://localhost:7106/api/v1
ServiceRequest: http://localhost:7107/api/v1
```

Add/update appsettings according to existing project conventions.

## Auth forwarding

All internal calls must forward:

```text
Authorization: Bearer {current Keycloak token}
X-Aizen-User-Token: {current Identity user token}
```

Do not hardcode tokens.
Read tokens from the current incoming HTTP context / headers according to the existing Aizen framework conventions.

## Remote service classes

Create/update typed remote service classes grouped by module if not already present.

```text
RemoteServices/Identity
RemoteServices/ReferenceData
RemoteServices/Vessel
RemoteServices/FileStorage
RemoteServices/ServiceRequest
```

Register all services in DependencyInjection.
