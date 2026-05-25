# Aizen InfoAccessor Container Trace and UserInfo Visibility Fix

## Purpose

`AizenUserInfoMiddleware` successfully reads `X-Aizen-User-Token`, creates a populated `userInfo`, and executes:

```csharp
container.Set(userInfo);
```

However, command/query handlers still cannot read the value through:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo
```

The issue is therefore not only JWT parsing. The real issue is that middleware writes user info into one storage path, while handlers read from another path, scope, instance, type, or container.

## Main Goal

Find the real architectural reason why the value set by `AizenUserInfoMiddleware` is not visible inside command/query handlers, then implement a clean Aizen-compatible fix.

## Must Inspect

- `Core/InfoAccessor/src/Aizen.Core.InfoAccessor`
- `Core/InfoAccessor/src/Aizen.Core.InfoAccessor.Abstraction`
- `AizenUserInfoMiddleware`
- `AizenInfoContainer`, `AizenInfoContainerForScoped`, `AizenInfoContainerForSigleton`
- `IAizenInfoContainer`
- `AizenInfoAccessor`, `IAizenInfoAccessor`
- `IAizenUserInfoAccessor`, `UserInfoAccessor`
- other accessors: server/channel info accessors
- DI registrations and lifetimes
- API/Operation/BFF starter registrations
- command/query dispatch infrastructure and any `CreateScope` usage

## Current Failing Code

```csharp
var userInfo = _infoAccessor.UserInfoAccessor.UserInfo;
if (userInfo is null || userInfo.UserId == 0)
    throw new AizenBusinessException(((int)AizenErrorCode.UserNotFound).ToString());
var userId = Convert.ToInt32(userInfo.UserId);
```

## Expected Result

- `container.Set(userInfo)` stores populated `AizenUserInfo` in the actual request-bound source of truth.
- `_infoAccessor.UserInfoAccessor.UserInfo` returns the same user info in command/query handlers.
- Command and query handlers both work.
- Missing/invalid user token returns a controlled business/auth error, not null reference.
