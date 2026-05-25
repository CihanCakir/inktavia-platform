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
