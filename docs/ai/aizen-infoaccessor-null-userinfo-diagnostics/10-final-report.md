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
