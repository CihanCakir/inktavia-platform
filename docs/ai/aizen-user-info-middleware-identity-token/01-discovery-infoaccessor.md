# 01 - Discovery: InfoAccessor

Do not modify code in this step.

Inspect:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

Search for:

```text
AizenUserInfoMiddleware
AizenUserInfo
AizenUserInfoAccessor
IAizenUserInfoAccessor
InfoAccessor
CurrentUser
UserId
Role
Version
RefreshTokenExpire
IsAuthenticated
IsSuccess
IsUser
IsClient
Claims
JwtSecurityTokenHandler
ReadJwtToken
```

## Required Questions

Answer:

### Middleware

- Where is `AizenUserInfoMiddleware`?
- How is it currently reading tokens?
- Does it read `Authorization` directly?
- Does it use `HttpContext.User.Claims`?
- What claim names does it currently expect?
- What happens if a claim is missing?
- Does it throw exceptions or silently skip?

### AizenUserInfo Model

- Where is `AizenUserInfo`?
- What properties exist?
- What flags exist?
- Which fields are required?
- Which fields are optional?
- Are there factory methods or setters?

### Accessor

- Which accessor interface stores/exposes `AizenUserInfo`?
- What is its lifetime?
- How does middleware set the current user info?
- Where is it registered in DI?

### Options

- Are there options for InfoAccessor?
- Is there a configurable header name?
- Is missing/invalid token behavior configurable?

## Required Output

Produce:

```md
# InfoAccessor Discovery Report

## Middleware Location
- ...

## Current Token Source
- ...

## Current Claim Parsing
- ...

## Current Error Behavior
- ...

## AizenUserInfo Properties
- ...

## AizenUserInfo Flags
- ...

## Accessor Interfaces
- ...

## DI Registration
- ...

## Missing Capabilities
- ...
```
