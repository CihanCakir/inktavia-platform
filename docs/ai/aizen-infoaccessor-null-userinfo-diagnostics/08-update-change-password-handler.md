# 08 - Update ChangePasswordCommandHandler

Update the failing handler safely.

## Target

```text
Modules/Identity/src/Aizen.Modules.Identity.Application/Common/Command/ChangePassword/ChangePasswordCommandHandler.cs
```

## Current Problem

Unsafe access:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo.UserId
```

causes `NullReferenceException` when `UserInfo` is null.

## Required Fix

Use a safe guard and project-standard exception handling.

Recommended shape:

```csharp
var userInfo = _infoAccessor.UserInfoAccessor?.UserInfo;

if (userInfo is null || userInfo.UserId <= 0)
{
    throw new AizenBusinessException(...);
}

var userId = userInfo.UserId;
```

Use the actual exception and error code pattern in the project.

## Error Code

Search for existing error codes:

```text
Unauthorized
UserNotAuthenticated
UserInfoNotFound
InvalidToken
AccessTokenRequired
```

If a suitable error exists, use it.

Only add a new error code if the project convention requires it.

## Do Not

Do not silently use `0` user id.

Do not return success if user context is missing.

Do not parse token again inside the handler.

The handler should consume InfoAccessor only.

## Required Output

Produce:

```md
# ChangePasswordCommandHandler Update

## Previous Problem
- ...

## New Guard
- ...

## Error Code Used
- ...

## Files Changed
- ...

## Why This Prevents NullReferenceException
- ...
```
