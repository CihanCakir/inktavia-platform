# AdminPanel BFF Corrective Contract

## Auth contract

All AdminPanel BFF endpoints receive user requests from AdminPanel clients.

All BFF internal calls must forward:

```text
Authorization: Bearer {Keycloak access token}
X-Aizen-User-Token: {Identity application user token}
```

Postman variables:

```text
Authorization: Bearer {{active_access_token}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
```

## Remote call contract

All internal API calls must use `AizenRemoteCall` according to the existing repository pattern.

Do not use raw `HttpClient` directly unless the current architecture requires `AizenRemoteCall` to wrap it.

## Active local endpoints

```text
Identity:       http://localhost:7101/api/v1
ReferenceData: http://localhost:7104/api/v1
Vessel:         http://localhost:7105/api/v1
FileStorage:    http://localhost:7106/api/v1
ServiceRequest: http://localhost:7107/api/v1
```

## BFF appsettings keys

Use environment-specific settings:

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

Adapt names to the existing configuration conventions if the repository already has a standard.
