# 02 - Remote Call, Auth Forwarding, and AppSettings

## Goal

Configure the Admin Panel BFF to call internal APIs through `AizenRemoteCall` and forward authentication headers consistently.

## Tasks

1. Search the repository for existing `AizenRemoteCall` usage.

Inspect patterns such as:

```text
AizenRemoteCall
IAizenRemoteCall
RemoteCall
RemoteServiceOptions
RemoteServiceClient
Aizen.Core.* RemoteCall
```

2. Reuse the existing convention. Do not invent an unrelated HTTP client wrapper.

3. Add or update Admin Panel BFF configuration.

Recommended configuration shape:

```json
{
  "RemoteServices": {
    "Identity": {
      "BaseUrl": "http://localhost:7101/api/v1"
    },
    "ReferenceData": {
      "BaseUrl": "http://localhost:7104/api/v1"
    },
    "Vessel": {
      "BaseUrl": "http://localhost:7105/api/v1"
    },
    "FileStorage": {
      "BaseUrl": "http://localhost:7106/api/v1"
    },
    "ServiceRequest": {
      "BaseUrl": "http://localhost:7107/api/v1"
    }
  }
}
```

4. Also support environment variable overrides using `.NET` double-underscore convention:

```text
RemoteServices__Identity__BaseUrl
RemoteServices__ReferenceData__BaseUrl
RemoteServices__Vessel__BaseUrl
RemoteServices__FileStorage__BaseUrl
RemoteServices__ServiceRequest__BaseUrl
```

5. Implement or reuse an auth forwarding component.

Possible names, if no equivalent exists:

```text
IAdminPanelBffAuthHeaderProvider
AdminPanelBffAuthHeaderProvider
IAdminPanelRemoteCallHeaderFactory
AdminPanelRemoteCallHeaderFactory
```

It must read incoming request headers:

```text
Authorization
X-Aizen-User-Token
```

and forward them to each internal API call.

6. If the existing framework already has `IAizenInfoAccessor`, `IHttpContextAccessor`, or remote-call header enrichment conventions, reuse those.

7. Ensure all internal calls use:

```text
Authorization: Bearer <incoming Keycloak token>
X-Aizen-User-Token: <incoming Identity token>
```

8. Add startup/DI registration.

9. Produce docs:

```text
Bff/src/AdminPanel/Aizen.Bff.AdminPanel/docs/admin-panel-bff-remote-call-auth.md
```

## Validation

- No hardcoded tokens.
- No hardcoded user IDs.
- No direct calls without forwarded auth headers.
- Appsettings has local defaults but can be overridden by environment variables.
