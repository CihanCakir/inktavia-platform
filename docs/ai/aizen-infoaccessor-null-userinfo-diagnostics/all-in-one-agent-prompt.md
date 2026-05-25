# All-in-One Agent Prompt - Aizen InfoAccessor Null UserInfo Diagnostics and Fix

Execute this task as a coding agent.

There is a `NullReferenceException` inside `ChangePasswordCommandHandler` when reading:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo.UserId
```

The Identity token appears to be parsed from:

```http
X-Aizen-User-Token: Bearer {identity_access_token}
```

but the command handler still sees `UserInfo` as null.

Your task is to find the exact root cause and fix it without breaking the existing Aizen architecture.

Do not apply only superficial null conditionals. Diagnose:

- InfoAccessor implementation
- UserInfoAccessor implementation
- DI lifetimes
- Middleware registration and order
- Token parsing assignment target
- Whether middleware and handler use the same scoped instance
- Handler guard/error handling

Follow every section in order.



---

# 00-context-and-error.md

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


---

# 01-discovery-infoaccessor-implementation.md

# 01 - Discovery: InfoAccessor Implementation

Do not modify code in this step.

Inspect:

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

Search for:

```text
InfoAccessor
IInfoAccessor
IAizenInfoAccessor
AizenInfoAccessor
UserInfoAccessor
IUserInfoAccessor
IAizenUserInfoAccessor
AizenUserInfoAccessor
AizenUserInfo
CurrentUser
UserInfo
SetUserInfo
GetUserInfo
```

## Required Questions

Answer:

### Interfaces

- What is the main InfoAccessor interface?
- What is the type of `_infoAccessor` used in command handlers?
- Does it expose `UserInfoAccessor`?
- What is the type of `UserInfoAccessor`?
- Does `UserInfoAccessor` expose `UserInfo` as nullable or non-nullable?

### Implementations

- Where is the InfoAccessor concrete implementation?
- Where is UserInfoAccessor concrete implementation?
- Does the implementation initialize `UserInfoAccessor`?
- Does the implementation initialize `UserInfo`?
- Are there setters or methods for setting user info?

### Models

- Where is `AizenUserInfo`?
- What properties and flags does it have?
- Does it have an empty/default instance factory?
- Does it have required constructor parameters?

## Required Output

Produce:

```md
# InfoAccessor Implementation Report

## Main Interface
- ...

## Concrete Implementation
- ...

## UserInfoAccessor Interface
- ...

## UserInfoAccessor Implementation
- ...

## UserInfo Initialization Behavior
- ...

## AizenUserInfo Model
- ...

## Null Risk Points
- ...
```


---

# 02-discovery-di-and-middleware-registration.md

# 02 - Discovery: DI and Middleware Registration

Do not modify code in this step.

Search for:

```text
AddAizenInfoAccessor
UseAizenInfoAccessor
AizenUserInfoMiddleware
UseMiddleware<AizenUserInfoMiddleware>
IHttpContextAccessor
AddHttpContextAccessor
services.AddScoped
services.AddSingleton
services.AddTransient
IAizenInfoAccessor
IAizenUserInfoAccessor
UserInfoAccessor
```

## Required Questions

### DI Registration

- Where is InfoAccessor registered?
- What lifetime is used?
- Where is UserInfoAccessor registered?
- What lifetime is used?
- Is `IHttpContextAccessor` registered?
- Is there more than one registration for the same interface?
- Does any module override the registration?

### Middleware Registration

- Where is `AizenUserInfoMiddleware` registered in the pipeline?
- Is it registered through `UseAizenInfoAccessor` or manually?
- Does the operation starter call it?
- What is its order relative to:
  - `UseRouting`
  - `UseAuthentication`
  - `UseAuthorization`
  - `MapControllers`

### Scope Consistency

- Does middleware receive the same scoped accessor instance that command handlers receive?
- Is the accessor accidentally singleton while holding request-specific state?
- Is the accessor transient, causing middleware and handler to get different instances?

## Required Output

Produce:

```md
# DI and Middleware Registration Report

## InfoAccessor Registration
- ...

## UserInfoAccessor Registration
- ...

## IHttpContextAccessor Registration
- ...

## Duplicate Registrations
- ...

## Middleware Registration
- ...

## Middleware Order
- ...

## Scope Consistency Assessment
- ...

## Suspected DI Issue
- ...
```


---

# 03-discovery-token-to-userinfo-flow.md

# 03 - Discovery: Token to UserInfo Flow

Do not modify code in this step.

Inspect:

```text
AizenUserInfoMiddleware
```

and related token readers/services.

## Required Questions

Answer:

### Token Header

- Does middleware read `X-Aizen-User-Token`?
- Does it support `Bearer` prefix?
- Does it still read from `Authorization` incorrectly?
- What happens if the header is missing?

### Token Parsing

- Which method parses the Identity token?
- Which claims are read?
- Is parsing successful?
- Are claim values logged or visible in debugger?
- Does parsing produce an `AizenUserInfo` instance?

### Assignment

This is critical.

Find exactly where parsed user info is assigned.

Examples:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo = userInfo;
```

or:

```csharp
context.Items["AizenUserInfo"] = userInfo;
```

or:

```csharp
_userInfoAccessor.SetUserInfo(userInfo);
```

Answer:

- Is parsed user info assigned to the DI accessor?
- Or only to `HttpContext.Items`?
- Does command handler read the same place?
- Is `UserInfoAccessor` initialized before assignment?
- Is assignment happening after `await _next(context)` instead of before?
- Is assignment skipped because of a flag/condition?

## Required Output

Produce:

```md
# Token to UserInfo Flow Report

## Header Reading
- ...

## Token Parsing
- ...

## Claims Read
- ...

## AizenUserInfo Created
- Yes / No

## Assignment Target
- DI accessor / HttpContext.Items / Local variable / Other

## Assignment Timing
- Before next middleware / After next middleware

## Why Handler Cannot See UserInfo
- ...
```


---

# 04-command-handler-null-reference-analysis.md

# 04 - Command Handler Null Reference Analysis

Do not modify code yet.

Inspect:

```text
Modules/Identity/src/Aizen.Modules.Identity.Application/Common/Command/ChangePassword/ChangePasswordCommandHandler.cs
```

Error line:

```text
line 38
```

The failing access is likely:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo.UserId
```

## Required Questions

Answer:

- What type is `_infoAccessor`?
- How is it injected?
- Is it the same interface registered by InfoAccessor package?
- Is `UserInfoAccessor` null?
- Is `UserInfo` null?
- Is `UserId` nullable or default?
- Is the handler executed during an HTTP request?
- Is the handler executed through MediatR/CQRS pipeline?
- Does the pipeline execute outside the HTTP request scope?
- Does any background job or bus handler use the same command?

## Required Safety Review

The handler should not use unsafe chains.

Instead of:

```csharp
var userId = _infoAccessor.UserInfoAccessor.UserInfo.UserId;
```

It should use a safe method or guard, for example:

```csharp
var userInfo = _infoAccessor.UserInfoAccessor?.UserInfo;

if (userInfo is null || userInfo.UserId <= 0)
{
    throw new AizenBusinessException(...);
}
```

Use the project's existing exception/error pattern.

## Required Output

Produce:

```md
# ChangePasswordCommandHandler Analysis

## Handler Path
- ...

## Injected InfoAccessor Type
- ...

## Failing Line
- ...

## Null Object
- _infoAccessor / UserInfoAccessor / UserInfo / UserId

## Execution Scope
- HTTP request / Background / Unknown

## Required Handler Guard
- ...

## Related Error Code Needed
- Existing / New
```


---

# 05-root-cause-decision-tree.md

# 05 - Root Cause Decision Tree

Use the discovery results to identify the exact root cause.

## Possible Root Causes

### Cause A - Wrong DI Lifetime

Middleware sets one instance, handler receives another.

Likely if accessor is registered as:

```csharp
Transient
```

for request-specific state.

Preferred:

```csharp
Scoped
```

### Cause B - Middleware Writes to HttpContext.Items Only

Middleware stores user info in:

```csharp
HttpContext.Items
```

but handler reads:

```csharp
_infoAccessor.UserInfoAccessor.UserInfo
```

Fix:

- Make accessor read from `HttpContext.Items`, or
- Middleware must set DI scoped accessor, or
- Both must use a single shared source of truth.

### Cause C - UserInfoAccessor Not Initialized

`_infoAccessor.UserInfoAccessor` or `.UserInfo` starts null and middleware tries to set nested property without ensuring object exists.

Fix:

- Initialize in constructor, or
- Provide setter method, or
- Use non-null default `AizenUserInfo.Empty`.

### Cause D - Middleware Order Wrong

Handler runs before middleware populates accessor.

Possible if middleware is not registered or registered after endpoint execution.

Fix:

- Register `AizenUserInfoMiddleware` before endpoint execution.

### Cause E - Header Missing in Postman

`X-Aizen-User-Token` is not being sent to the ChangePassword request.

Fix:

- Confirm Postman environment and request headers.
- Middleware should still throw controlled error, not NullReference.

### Cause F - Token Parsing Success But Assignment Skipped

Middleware parses claims but does not assign due to flag condition or invalid check.

Fix:

- Adjust assignment condition.

### Cause G - Handler Executed Outside HTTP Scope

If command handler is invoked by background worker/message bus, no HTTP context exists.

Fix:

- Define non-HTTP identity propagation strategy or guard.

## Required Output

Produce:

```md
# Root Cause Decision

## Selected Root Cause
- ...

## Evidence
- ...

## Fix Strategy
- ...

## Files To Change
- ...

## Risks
- ...
```


---

# 06-fix-design.md

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


---

# 07-implement-safe-infoaccessor-flow.md

# 07 - Implement Safe InfoAccessor Flow

Implement the root-cause fix.

## Target Area

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor
```

## Required Implementation Checks

### 1. DI Lifetime

Ensure request-scoped state services are registered as scoped.

Examples:

```csharp
services.AddScoped<IAizenInfoAccessor, AizenInfoAccessor>();
services.AddScoped<IAizenUserInfoAccessor, AizenUserInfoAccessor>();
```

Use actual interface/class names.

Do not register request-scoped user state as singleton.

### 2. Accessor Initialization

Ensure `UserInfoAccessor` is initialized.

If `AizenInfoAccessor` contains `UserInfoAccessor`, initialize it in constructor or inject it.

Example:

```csharp
public AizenInfoAccessor(IAizenUserInfoAccessor userInfoAccessor)
{
    UserInfoAccessor = userInfoAccessor;
}
```

### 3. UserInfo Assignment

Ensure middleware assigns parsed user info to the same scoped accessor consumed by handlers.

Example:

```csharp
_userInfoAccessor.SetUserInfo(userInfo);
```

or:

```csharp
_infoAccessor.UserInfoAccessor.SetUserInfo(userInfo);
```

Use actual architecture.

### 4. Missing Header

If `X-Aizen-User-Token` is missing:

- Do not throw NullReference.
- Clear/set empty user info.
- Continue or throw controlled error depending on existing options.

### 5. Invalid Token

If invalid:

- Controlled behavior.
- No raw token logging.

### 6. Middleware Timing

Ensure assignment happens before:

```csharp
await _next(context);
```

if downstream handlers need the value.

## Required Output

Produce:

```md
# Safe InfoAccessor Flow Implementation

## DI Lifetime Changes
- ...

## Initialization Changes
- ...

## Middleware Assignment Changes
- ...

## Missing Token Behavior
- ...

## Invalid Token Behavior
- ...

## Files Changed
- ...
```


---

# 08-update-change-password-handler.md

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


---

# 09-tests-and-verification.md

# 09 - Tests and Verification

Add or update tests if possible.

## Required Test Scenarios

### 1. Valid X-Aizen-User-Token

Request:

```http
Authorization: Bearer {keycloak_api_client_token}
X-Aizen-User-Token: Bearer {identity_access_token}
```

Expected:

- Middleware sets user info.
- `ChangePasswordCommandHandler` reads user id.
- No `NullReferenceException`.

### 2. Missing X-Aizen-User-Token

Request:

```http
Authorization: Bearer {keycloak_api_client_token}
```

Expected:

- No `NullReferenceException`.
- Handler returns controlled auth/business error.

### 3. Invalid X-Aizen-User-Token

Expected:

- No `NullReferenceException`.
- Controlled error or configured behavior.

### 4. DI Scope Consistency

Verify middleware and handler use same scoped accessor instance.

Possible test:

- Set accessor in scope.
- Resolve handler in same scope.
- Confirm handler sees user info.

### 5. Postman Verification

Confirm ChangePassword request contains:

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

## Commands

Run:

```bash
dotnet build
dotnet test
```

## Required Output

Produce:

```md
# Verification Report

## Build
- ...

## Tests
- ...

## Valid Token Scenario
- ...

## Missing User Token Scenario
- ...

## Invalid User Token Scenario
- ...

## DI Scope Verification
- ...

## Postman Header Verification
- ...
```


---

# 10-final-report.md

# 10 - Final Report

Produce the final report in this exact format:

```md
# Final Report - InfoAccessor Null UserInfo Fix

## 1. Root Cause

- ...

## 2. Error Location

- Handler:
- Line:
- Failing access:

## 3. InfoAccessor Implementation

- Interface:
- Implementation:
- UserInfoAccessor:
- AizenUserInfo:

## 4. DI Registration

- InfoAccessor lifetime:
- UserInfoAccessor lifetime:
- IHttpContextAccessor:
- Duplicate registrations:

## 5. Middleware Flow

- Token header:
- Token parsing:
- UserInfo assignment target:
- Assignment timing:

## 6. Fix Applied

- DI:
- Initialization:
- Middleware:
- Handler guard:
- Error handling:

## 7. Changed Files

- `path/to/file.cs`
  - ...

## 8. Verification

### Build
- ...

### Tests
- ...

### Manual Postman
- ...

## 9. Request Contract

```http
Authorization: Bearer {keycloak_api_client_token}
X-Aizen-User-Token: Bearer {identity_access_token}
```

## 10. Follow-up

- ...
```
