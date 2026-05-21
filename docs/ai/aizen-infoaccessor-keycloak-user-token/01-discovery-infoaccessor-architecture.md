# 01 - Discovery: InfoAccessor Architecture

Do not modify code in this step.

Inspect:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

Search for:

```text
AizenUserInfoMiddleware
InfoAccessor
UserInfo
CurrentUser
IAizenUserInfoAccessor
IAizenInfoAccessor
IUserInfoAccessor
AizenUserInfo
AizenUserContext
ClaimsPrincipal
HttpContext
Authorization
Bearer
JwtSecurityTokenHandler
ReadJwtToken
FindFirst
userId
sub
version
refreshTokenExpire
```

## Required Questions

### Middleware

- Where is `AizenUserInfoMiddleware` located?
- What does it do?
- Which token/header does it read?
- What claims does it expect?
- What exception occurs when claims are missing?
- Does it fail hard or silently?
- Is it registered in the application pipeline?

### Accessor Interfaces

- Which interfaces expose user info?
- What models/classes are used for user info?
- Are they scoped/singleton/transient?
- Which parts of the application consume them?

### Current Claim Contract

Find current user token claim expectations. Document actual claim names from code.

### Token Source

Determine whether the middleware reads `Authorization`, `HttpContext.User.Claims`, custom header, cookie, query string, or something else.

### DI and Starter

Find where InfoAccessor is registered. Search:

```text
AddAizenInfoAccessor
UseAizenInfoAccessor
AizenInfoAccessor
InfoAccessorServiceCollectionExtensions
InfoAccessorApplicationBuilderExtensions
```

## Required Output

Produce:

```md
# InfoAccessor Discovery Report

## Middleware Location
- ...

## Middleware Current Behavior
- ...

## Current Token Source
- ...

## Expected Claims
- ...

## Current Failure Reason
- ...

## Accessor Interfaces and Models
- ...

## DI Registration
- ...

## Application Pipeline Registration
- ...

## Current Consumers
- ...

## Recommended Extension Point
- ...
```
