# 00 - Context and Observation

## Context

The runtime request uses:

```http
Authorization: Bearer {keycloak_api_client_token}
X-Aizen-User-Token: Bearer {identity_access_token}
```

`AizenUserInfoMiddleware` reads `X-Aizen-User-Token` correctly. The token is parsed and `userInfo` is populated.

## Current Observation

Inside middleware:

```csharp
container.Set(userInfo);
```

receives populated data.

Inside command/query handlers:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo
```

is still null.

## Main Investigation Rule

Do not solve this only by adding null checks. Find why middleware write and handler read are not using the same request-bound storage.

## Output Required

```md
# Initial Understanding

## Middleware Write
- Is userInfo populated before container.Set?
- Where is Set called?

## Handler Read
- Which handler reads _infoAccessor.UserInfoAccessor.UserInfo?
- Is UserInfoAccessor null or UserInfo null?

## Main Hypotheses
- Different container instance:
- Different DI scope:
- Type mismatch:
- Accessor reads different source:
- Middleware timing:
```
