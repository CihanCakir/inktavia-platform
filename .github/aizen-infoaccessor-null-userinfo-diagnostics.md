# Aizen InfoAccessor Null UserInfo Diagnostics and Fix

## Purpose

There is a runtime `NullReferenceException` in a command handler when accessing:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo.UserId
```

The Identity token is sent and the middleware appears to read/populate values from the token, but inside the command handler `UserInfo` is null.

The failing handler is:

```text
Modules/Identity/src/Aizen.Modules.Identity.Application/Common/Command/ChangePassword/ChangePasswordCommandHandler.cs
```

Error:

```text
System.NullReferenceException: Object reference not set to an instance of an object.
at Aizen.Modules.InktaviaStore.Application.Identity.Command.ChangePasswordCommandHandler.<Handle>d__6.MoveNext()
```

The suspected issue is that the InfoAccessor implementation or DI/middleware registration is not correctly scoped, not correctly set, or the command handler is resolving a different instance than the middleware populates.

## Main Goal

Find exactly where InfoAccessor is implemented, where it is registered, where it is populated, and why `UserInfo` is null inside `ChangePasswordCommandHandler`.

Then implement a clean fix without breaking the existing Aizen architecture.

## Required Investigation Areas

Inspect:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

Especially:

```text
AizenUserInfoMiddleware
AizenUserInfo
UserInfoAccessor
IAizenInfoAccessor
IAizenUserInfoAccessor
AizenInfoAccessor
AddAizenInfoAccessor
UseAizenInfoAccessor
```

Also inspect:

```text
Modules/Identity/src/Aizen.Modules.Identity.Application/Common/Command/ChangePassword/ChangePasswordCommandHandler.cs
```

And the request flow:

```http
Authorization: Bearer {keycloak_api_client_token}
X-Aizen-User-Token: Bearer {identity_access_token}
```

## Expected Result

The command handler should be able to safely read the user id from the user info context after the middleware parses `X-Aizen-User-Token`.

The fix should avoid unsafe chains like:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo.UserId
```

unless all intermediate objects are guaranteed non-null.

## Required Final Behavior

- InfoAccessor is registered with the correct lifetime.
- Middleware populates the same scoped accessor instance used by command handlers.
- `UserInfoAccessor` is not null.
- `UserInfo` is not null when a valid `X-Aizen-User-Token` exists.
- Missing/invalid user token produces a controlled business/auth error, not a `NullReferenceException`.
- `ChangePasswordCommandHandler` handles missing user info safely.
