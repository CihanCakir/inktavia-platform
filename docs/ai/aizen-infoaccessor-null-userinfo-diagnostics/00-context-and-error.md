# 00 - Context and Error

## Context

The system now uses two tokens:

```http
Authorization: Bearer {keycloak_api_client_token}
X-Aizen-User-Token: Bearer {identity_access_token}
```

The Keycloak token protects the API.

The Identity token is read by `AizenUserInfoMiddleware` to populate `AizenUserInfo`.

## Current Error

A command handler throws:

```text
System.NullReferenceException: Object reference not set to an instance of an object.
```

Failing location:

```text
Modules/Identity/src/Aizen.Modules.Identity.Application/Common/Command/ChangePassword/ChangePasswordCommandHandler.cs:line 38
```

Failing access pattern:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo.UserId
```

## Observed Behavior

The token appears to be read from the request and values appear to be populated from token parsing.

However, inside the command handler, `UserInfo` is null.

## Main Objective

Find why the populated user info is not available inside the command handler.

## Do Not

Do not apply a superficial null-conditional fix only.

Do not simply change:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo.UserId
```

to:

```csharp
_infoAccessor?.UserInfoAccessor?.UserInfo?.UserId
```

without identifying the root cause.

The goal is to fix the InfoAccessor request-scoped flow correctly.

## Required Output Before Code Changes

Produce:

```md
# Initial Error Understanding

## Failing Handler
- ...

## Failing Access Chain
- ...

## Expected Source of UserInfo
- ...

## Suspected Root Causes
- ...

## Investigation Plan
- ...
```
