# All-in-One Agent Prompt - Aizen InfoAccessor Container Trace and Fix

Execute this task as a coding agent.

Critical context: `AizenUserInfoMiddleware` already reads `X-Aizen-User-Token`, creates a populated `userInfo`, and calls `container.Set(userInfo)`. Despite that, command/query handlers still get null from `_infoAccessor.UserInfoAccessor.UserInfo`.

This is a container/accessor/scope/type propagation problem. Do not only add null conditionals. Diagnose and fix the root cause.

Follow every section in order.


---

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


---

# 01 - Discovery: Container and Accessor Architecture

Do not modify code yet.

Inspect:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
Core/InfoAccessor/src/Aizen.Core.InfoAccessor.Abstraction
```

Search for:

```text
IAizenInfoContainer
AizenInfoContainer
AizenInfoContainerForScoped
AizenInfoContainerForSigleton
AizenInfoAccessor
IAizenInfoAccessor
IAizenUserInfoAccessor
AizenUserInfoAccessor
AizenUserInfo
Set(
Get<
```

## Questions

### Container

- How does `IAizenInfoContainer.Set<T>` work?
- How does `IAizenInfoContainer.Get<T>` work?
- Is `typeof(T)` used as key?
- Is storage instance-level, static, `AsyncLocal`, `HttpContext.Items`, or dictionary?
- What is the difference between scoped and singleton container implementations?

### Accessors

- What is the concrete `IAizenInfoAccessor`?
- What is the concrete `IAizenUserInfoAccessor`?
- Does `UserInfoAccessor.UserInfo` read `container.Get<AizenUserInfo>()`?
- Is `UserInfoAccessor` injected into `AizenInfoAccessor` or manually constructed?
- Is `UserInfo` initialized to a null-object or left nullable?

### Type Identity

Search all definitions:

```bash
find . -name "*.cs" -exec grep -n "class AizenUserInfo\|record AizenUserInfo" {} \;
```

Check whether middleware sets one `AizenUserInfo` type while accessor gets another namespace/assembly type.

## Output Required

```md
# Container and Accessor Architecture Report

## Container Implementations
- ...

## Set/Get Mechanism
- ...

## InfoAccessor Implementation
- ...

## UserInfoAccessor Implementation
- ...

## AizenUserInfo Type Identity
- ...

## Mismatch Risks
- ...
```


---

# 02 - Trace Set/Get Type and Instance

Add temporary diagnostic logs or debugger-only traces if needed. Remove or convert to safe debug logs after fixing.

## Goal

Prove whether middleware and handler use the same:

- container instance
- DI scope
- `AizenUserInfo` type
- accessor path

## Trace in AizenUserInfoMiddleware

Immediately before and after `container.Set(userInfo)`, inspect/log safely:

```csharp
container.GetType().FullName
container.GetHashCode()
userInfo.GetType().FullName
userInfo.GetType().Assembly.FullName
userInfo.UserId
container.Get<AizenUserInfo>() immediately after Set
HttpContext.TraceIdentifier
```

Do not log raw tokens.

## Trace in UserInfoAccessor.UserInfo getter

Inspect/log safely:

```csharp
container.GetType().FullName
container.GetHashCode()
typeof(AizenUserInfo).FullName
typeof(AizenUserInfo).Assembly.FullName
container.Get<AizenUserInfo>() result
HttpContext.TraceIdentifier if accessible
```

## Trace in ChangePasswordCommandHandler

Inspect/log safely:

```csharp
_infoAccessor.GetType().FullName
_infoAccessor.UserInfoAccessor?.GetType().FullName
_infoAccessor.UserInfoAccessor?.UserInfo is null
HttpContext.TraceIdentifier if IHttpContextAccessor is available
```

## Interpretation

- If immediate Get after Set fails: container Set/Get or type mismatch.
- If immediate Get succeeds but accessor fails: different container instance/source/type.
- If accessor works in middleware but not handler: DI scope or CQRS child scope.
- If type assembly differs: duplicate `AizenUserInfo` type.

## Output Required

```md
# Trace Report

## Middleware Container
- Type:
- Hash:
- Immediate Get:

## Accessor Container
- Type:
- Hash:
- Get:

## Handler Accessor
- Type:
- UserInfo:

## Type Identity
- Middleware type:
- Accessor type:

## Conclusion
- ...
```


---

# 03 - Discovery: DI Lifetime and Scope

Do not modify code until the registration map is clear.

Search for all registrations of:

```text
IAizenInfoContainer
AizenInfoContainer
AizenInfoContainerForScoped
AizenInfoContainerForSigleton
IAizenInfoAccessor
IAizenUserInfoAccessor
AddInfoAccessor
AddAizenInfoAccessor
AddScoped
AddTransient
AddSingleton
```

## Questions

- What lifetime is used for `IAizenInfoContainer`?
- What lifetime is used for `IAizenInfoAccessor`?
- What lifetime is used for `IAizenUserInfoAccessor`?
- Are there duplicate registrations?
- Which registration wins?
- Are API, Operation, BFF starters registering different implementations?
- Does any module override these registrations?

## Correct Direction

Request-specific user info should be request-bound. Usually this means:

```csharp
services.AddScoped<IAizenInfoContainer, AizenInfoContainerForScoped>();
services.AddScoped<IAizenInfoAccessor, AizenInfoAccessor>();
services.AddScoped<IAizenUserInfoAccessor, AizenUserInfoAccessor>();
```

But follow existing project conventions.

## Output Required

```md
# DI Lifetime Report

## Registrations Found
- ...

## Duplicate Registrations
- ...

## API/Operation/BFF Differences
- ...

## Lifetime Issue
- ...
```


---

# 04 - Discovery: CQRS / Handler Scope

Middleware may set a scoped container, but handlers may be resolved in a different scope.

Inspect CQRS/mediator infrastructure:

```text
Core/CQRS
Core/Mediator
Core/Application
CommandHandler
QueryHandler
Send
Dispatch
IServiceProvider.CreateScope
CreateAsyncScope
IMediator
MediatR
PipelineBehavior
UnitOfWork
```

## Questions

- How are controllers dispatching commands/queries?
- Does command/query dispatch create a new `IServiceScope`?
- Does any pipeline behavior create a new scope?
- Does UnitOfWork create a new scope?
- Are handlers resolved from root provider, request provider, or child provider?
- Is `_infoAccessor` resolved in the same request scope as middleware?

## If a Child Scope Exists

Possible fixes:

1. Avoid creating a new scope for request command/query dispatch.
2. Propagate AizenInfo values to the child scope.
3. Make `UserInfoAccessor` read from `HttpContext.Items` via `IHttpContextAccessor`.
4. Use existing AsyncLocal request context if project already has one.

Prefer the least invasive architecture-compatible fix.

## Output Required

```md
# Handler Scope Report

## Dispatch Path
- ...

## Scope Creation Points
- ...

## Handler Lifetime
- ...

## Same Scope As Middleware?
- Yes/No

## Scope Root Cause
- ...
```


---

# 05 - Compare Other InfoAccessors

Inspect server/channel/accessor patterns that already work.

Search:

```text
IAizenServerInfoAccessor
IAizenChannelInfoAccessor
ServerInfo
ChannelInfo
AizenInfoMiddleware
```

## Questions

- How are server/channel infos set?
- Do they use `IAizenInfoContainer`?
- Are they visible inside handlers?
- Are their accessors using the same DI lifetime as user accessor?
- Do they read from config, container, HttpContext, static, or AsyncLocal?
- What pattern should user info follow?

## Output Required

```md
# Other Accessor Comparison

## ServerInfo Pattern
- ...

## ChannelInfo Pattern
- ...

## UserInfo Pattern
- ...

## Difference Causing Problem
- ...

## Pattern To Reuse
- ...
```


---

# 06 - Root Cause Matrix

Use evidence to select the exact cause.

## Possible Causes

### A - Different Container Instance
Evidence: middleware container hash differs from accessor/handler container hash.

### B - Different DI Scope
Evidence: handler is resolved in a new scope, so scoped container state is lost.

### C - Type Mismatch
Evidence: middleware `AizenUserInfo` type/assembly differs from accessor `AizenUserInfo` type/assembly.

### D - Accessor Reads Different Source
Evidence: middleware writes container, accessor reads field/other container/HttpContext.Items, or reverse.

### E - Middleware Timing
Evidence: `container.Set(userInfo)` occurs after `_next(context)`.

### F - Middleware Not in Actual Pipeline
Evidence: middleware does not run for the failing endpoint.

### G - Container Implementation Bug
Evidence: immediate `Get` after `Set` fails in the same middleware.

## Output Required

```md
# Root Cause Matrix Result

## Confirmed Cause
- ...

## Evidence
- ...

## Rejected Causes
- ...

## Fix Direction
- ...
```


---

# 07 - Fix Design and Implementation

Design first, then implement.

## Preferred Architecture

```text
AizenUserInfoMiddleware
  -> reads X-Aizen-User-Token
  -> creates AizenUserInfo
  -> writes to a single request-bound source of truth

IAizenUserInfoAccessor.UserInfo
  -> reads the same source

CommandHandler / QueryHandler
  -> reads IAizenInfoAccessor / IAizenUserInfoAccessor safely
```

## Candidate Fixes

### Option 1 - Scoped Container Fix
Use if middleware and handler should share the same request scope.

- Ensure `IAizenInfoContainer` is scoped.
- Ensure `IAizenInfoAccessor` and `IAizenUserInfoAccessor` are scoped.
- Ensure middleware and handlers resolve from same request scope.

### Option 2 - HttpContext.Items-backed UserInfoAccessor
Use if handlers may be resolved in child scopes but `IHttpContextAccessor` is available.

- Middleware writes to both container and `HttpContext.Items[AizenUserInfoKey]`.
- `UserInfoAccessor.UserInfo` first reads `HttpContext.Items`.
- Fallback to container.

### Option 3 - Context Propagation to Child Scope
Use if CQRS intentionally creates a child scope.

- Capture `AizenUserInfo` from parent/request context.
- Copy into child scope container before handler execution.

### Option 4 - Type Normalization
Use if type mismatch is confirmed.

- Use one canonical `AizenUserInfo` type in middleware and accessor.

## Implementation Rules

- Do not use static mutable global user state.
- Do not parse token again in handlers.
- Do not make Keycloak token act as user token.
- Preserve Aizen architecture.
- Keep missing-user behavior controlled.

## Output Required

```md
# Fix Implementation Result

## Selected Option
- ...

## Why
- ...

## Changed Files
- ...

## Storage Source of Truth
- ...

## DI / Scope Changes
- ...

## Accessor Changes
- ...
```


---

# 08 - Handler Usage and Verification

After fixing root cause, update unsafe handler access patterns.

## Search

```text
_infoAccessor.UserInfoAccessor.UserInfo
.UserInfoAccessor.UserInfo.UserId
```

## Required Pattern

Handlers should not rely on unsafe nested access without guard.

Preferred:

```csharp
var userInfo = _infoAccessor.UserInfoAccessor.UserInfo;
if (userInfo is null || userInfo.UserId == 0)
    throw new AizenBusinessException(((int)AizenErrorCode.UserNotFound).ToString());
var userId = Convert.ToInt32(userInfo.UserId);
```

Better if consistent with architecture:

```csharp
var userId = _infoAccessor.UserInfoAccessor.GetRequiredUserId();
```

Only add a helper if it does not break existing abstractions.

## Verify

Headers:

```http
Authorization: Bearer {{keycloakAccessToken}}
X-Aizen-User-Token: {{X-Aizen-User-Token}}
Content-Type: application/json
```

Body:

```json
{
  "oldPassword": "123456",
  "newPassword": "NewPassword123!",
  "newPasswordComfirm": "NewPassword123!"
}
```

Run:

```bash
dotnet build
dotnet test
```

## Output Required

```md
# Verification Result

## Unsafe Usages Found
- ...

## Updated Handlers
- ...

## Build
- ...

## Tests
- ...

## Manual Request Result
- ...
```


---

# 09 - Final Report

Produce final report exactly like this:

```md
# Final Report - Aizen InfoAccessor Container Visibility Fix

## 1. Problem Summary

- Middleware populated userInfo:
- Handler received null:

## 2. Root Cause

- ...

## 3. Evidence

### Middleware
- Container type:
- Container instance/hash:
- Immediate Get after Set:

### Accessor
- Accessor type:
- Container type/hash:
- UserInfo result:

### Handler
- Handler:
- Accessor type:
- UserInfo result:

### Scope
- Same scope / child scope:

### Type Identity
- Middleware AizenUserInfo type:
- Accessor AizenUserInfo type:

## 4. Fix Applied

- ...

## 5. Changed Files

- `path/to/file.cs`
  - ...

## 6. Architecture Decision

- Storage source of truth:
- DI lifetime:
- CQRS scope handling:
- Accessor read strategy:

## 7. Verification

### Build
- ...

### Tests
- ...

### Manual Postman
- ...

## 8. Final Request Contract

```http
Authorization: Bearer {keycloak_api_client_token}
X-Aizen-User-Token: Bearer {identity_access_token}
```

## 9. Follow-up

- ...
```
