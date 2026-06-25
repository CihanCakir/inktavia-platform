# ServiceRequest BFF Auth and Token Forwarding Report

## Scope

Auth boundary audit for ServiceRequest integration in AdminPanel BFF.

## Auth Flow

### Browser → AdminPanel BFF

```http
X-Aizen-User-Token: Bearer <identityAccessToken>
```

- BFF authenticates via Identity token (`X-Aizen-User-Token`)
- No browser-provided Keycloak token expected or accepted
- Controller uses `[Authorize(Policy = "AdminPanelAccess")]`

### AdminPanel BFF → ServiceRequest Module

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

Both headers are forwarded on every internal call.

## Token Acquisition Pattern

**In every BFF query handler:**
```csharp
var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
var authHeader = $"Bearer {serviceToken}";
```

`IAdminPanelBffKeycloakServiceTokenProvider` acquires and caches the Keycloak client_credentials token for `admin-panel-bff`.

## User Token Forwarding

**In every BFF controller action:**
```csharp
var userToken = HttpContext.Request.Headers["X-Aizen-User-Token"].FirstOrDefault() ?? string.Empty;
```

Passed to query/command, then forwarded as `X-Aizen-User-Token` in the remote call.

## ServiceRequest Module Auth

ServiceRequest admin endpoints require `[Authorize(Roles = "Admin")]`.

The ServiceRequest module validates:
1. `Authorization` header (Keycloak service token) — validates BFF is an authorized caller
2. The user identity is derived from `X-Aizen-User-Token` via the Aizen user claims transformation

## Policy Boundary

| Request Type | Expected Result |
|-------------|----------------|
| No token | 401 |
| Customer/participant Identity token | 403 (fails `AdminPanelAccess` policy) |
| Valid admin Identity token | 200 |
| BFF → SR without service token | 401 from SR module |
| BFF → SR with expired user token | 401/403 from SR module |

## Handler Null-Safety

All BFF query handlers for ServiceRequest are wrapped in try/catch:
```csharp
catch
{
    response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ServiceRequest"));
}
```

This ensures BFF always returns a valid response structure even when SR module is down.

## No Forwarding of Incoming Authorization Header

BFF does not forward the incoming `Authorization` header to internal APIs. The service token is independently acquired server-side.
