# Identity Device Session Cache Boundary

## Identity remains the user authority

Identity owns login, refresh, user profile, device context, and panel/domain authorization.

The BFF must not silently extend or invent Identity tokens.

## Device-aware cache is optional and contract-bound

Create a device-aware Identity session/cache only if it fits the current Identity module contract.

Use existing accessors where available:

```text
IAizenInfoAccessor
UserInfo
Client
Device
DeviceId
ClientId
ApplicationContext
```

## Suggested cache keys

Do not use raw tokens as Redis keys.

Suggested formats:

```text
inktavia:admin-panel-bff:identity-session:{sessionId}
inktavia:admin-panel-bff:identity-session-by-device:{userIdHash}:{deviceIdHash}
inktavia:admin-panel-bff:identity-token-context:{identityAccessTokenHash}
```

## Suggested DTO

```csharp
public sealed class CachedAdminIdentitySession
{
    public Guid UserId { get; init; }
    public string? UserName { get; init; }
    public string? PanelContext { get; init; }
    public string? DeviceId { get; init; }
    public string? ClientId { get; init; }
    public DateTimeOffset IdentityAccessTokenExpiresAtUtc { get; init; }
    public DateTimeOffset? IdentityRefreshTokenExpiresAtUtc { get; init; }
    public string? ProtectedIdentityAccessToken { get; init; }
    public string? ProtectedIdentityRefreshToken { get; init; }
}
```

Only store protected token values if the repository has an approved protection/encryption mechanism.

Otherwise, store non-sensitive metadata only.

## Refresh boundary

Identity token refresh must go through the real Identity refresh endpoint.

If BFF has a refresh token and device/session context, it may call Identity refresh.

If BFF does not have a refresh token, it should return 401 and let the React client call the real BFF `/auth/refresh` endpoint.

Do not bypass Identity validation.
