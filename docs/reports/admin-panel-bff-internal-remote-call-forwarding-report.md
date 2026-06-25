# AdminPanel BFF Internal RemoteCall Forwarding Report

## Report Date
2026-06-10

---

## Target Header Model (Post-Refactor)

Every internal API call from AdminPanel BFF now sends:

```http
Authorization: Bearer <admin-panel-bff-keycloak-service-token>
X-Aizen-User-Token: Bearer <identityAccessToken>
```

---

## Source of Each Header

| Header | Source |
|---|---|
| `Authorization` | `IAdminPanelBffKeycloakServiceTokenProvider.GetAccessTokenAsync()` — server-side, cached via `IAizenDistributedCache` |
| `X-Aizen-User-Token` | Extracted from incoming browser request header `X-Aizen-User-Token`, forwarded unchanged |

---

## RemoteCall Interfaces (unchanged signatures)

The RemoteCall interface method signatures were NOT changed. They still accept:
```csharp
[AizenRemoteCallHeader("Authorization")] string authorization,
[AizenRemoteCallHeader("X-Aizen-User-Token")] string userToken
```

What changed is the **source** of the `authorization` value — now comes from the cached service token provider, not the browser.

| Interface | Methods updated (source of Authorization) |
|---|---|
| `IIdentityAdminBffRemoteCall` | All profile/admin methods now receive BFF service token |
| `IVesselAdminBffRemoteCall` | All vessel methods now receive BFF service token |
| `IFileStorageAdminBffRemoteCall` | All file methods now receive BFF service token |
| `IReferenceDataAdminBffRemoteCall` | All reference data methods now receive BFF service token |
| `IServiceRequestAdminBffRemoteCall` | All service request methods now receive BFF service token |

Auth endpoints (login/refresh/otp) do NOT send Authorization — correct, these are public and unauthenticated at the Identity level.

---

## Handler Pattern (Post-Refactor)

Every handler that calls a RemoteCall now:

```csharp
public sealed class SomeQueryHandler : AizenQueryHandler<SomeQuery, SomeResult>
{
    private readonly ISomeRemoteCall _remote;
    private readonly IAdminPanelBffKeycloakServiceTokenProvider _serviceTokenProvider;

    public SomeQueryHandler(ISomeRemoteCall remote, IAdminPanelBffKeycloakServiceTokenProvider serviceTokenProvider)
    {
        _remote = remote;
        _serviceTokenProvider = serviceTokenProvider;
    }

    public override async Task<SomeResult?> Handle(SomeQuery request, CancellationToken cancellationToken)
    {
        var serviceToken = await _serviceTokenProvider.GetAccessTokenAsync(cancellationToken);
        var authHeader = $"Bearer {serviceToken}";
        var r = await _remote.SomeMethod(authHeader, request.UserToken, ...);
        return r.Body;
    }
}
```

---

## Command/Query Objects (Post-Refactor)

`Authorization` property has been removed from all command/query objects. Only `UserToken` (Identity token) remains where user context is needed.

**Affected files count:** 49 command/query .cs files updated.

---

## What Was Forbidden and Is Now Correct

| Forbidden | Status |
|---|---|
| Forward incoming browser `Authorization` header to internal APIs | ✅ Removed |
| Require browser Keycloak token | ✅ Removed — browsers only need `X-Aizen-User-Token` |
| Call Keycloak token endpoint per internal API call | ✅ Prevented — cache-backed provider reuses token |
| Call internal APIs without `X-Aizen-User-Token` (where user context required) | ✅ Identity token forwarded on all protected calls |
