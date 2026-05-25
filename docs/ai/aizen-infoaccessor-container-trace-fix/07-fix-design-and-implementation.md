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
