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
