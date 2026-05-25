# 06 - Fix Design

Design the fix before implementation.

## Preferred Architecture

The InfoAccessor layer should have one clear source of truth for request-scoped user info.

Recommended:

```text
AizenUserInfoMiddleware reads X-Aizen-User-Token
-> creates AizenUserInfo
-> sets it into a scoped IAizenUserInfoAccessor
-> command handlers read the same scoped accessor
```

## Required Principles

- Request-specific user info must be scoped.
- Middleware and command handlers must use the same scoped accessor instance.
- `UserInfo` should not cause `NullReferenceException`.
- Missing user context should produce a controlled business/auth exception.
- Keycloak token must remain separate.
- Do not parse `Authorization` as user identity.

## Recommended API Shape

If existing structure allows, add methods such as:

```csharp
public interface IAizenUserInfoAccessor
{
    AizenUserInfo? UserInfo { get; }
    bool HasUserInfo { get; }
    void SetUserInfo(AizenUserInfo userInfo);
    void Clear();
}
```

If existing interface cannot change safely, implement equivalent behavior using existing members.

## Default Object Strategy

Consider one of these:

### Option A - Nullable with Guard

`UserInfo` can be null, but every consumer must guard.

### Option B - Null Object

`UserInfo` is never null and defaults to:

```csharp
AizenUserInfo.Empty
```

Then flags indicate availability:

```csharp
IsUserTokenAvailable = false
IsUserTokenValid = false
IsAuthenticated = false
```

Prefer the option that best fits existing architecture.

## Handler Strategy

In `ChangePasswordCommandHandler`, do not use unsafe nested access.

Use a safe guard:

```csharp
var userInfo = _infoAccessor.UserInfoAccessor?.UserInfo;

if (userInfo is null || userInfo.UserId <= 0)
{
    throw new AizenBusinessException(...);
}
```

Use existing error codes if available.

## Required Output

Produce:

```md
# Fix Design

## Source of Truth
- ...

## Lifetime Strategy
- ...

## UserInfo Null Strategy
- Nullable with guard / Null object

## Middleware Assignment Strategy
- ...

## Handler Guard Strategy
- ...

## Error Handling Strategy
- ...

## Files To Change
- ...
```
