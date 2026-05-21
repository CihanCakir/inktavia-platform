# 09 - Final Report

Produce final report in this format:

```md
# Final Report - AizenUserInfoMiddleware Identity Token Header Mapping

## 1. Root Cause

- ...

## 2. Token Contract

### Keycloak API Token

- Header:
- Purpose:

### Identity User Token

- Header:
- Purpose:

## 3. CreateLoginToken Analysis

- Location:
- userInfo values:
- _tokenHelper method:
- Claims produced:

## 4. Claim Mapping

| AizenUserInfo Field | Claim | Notes |
|---|---|---|

## 5. AizenUserInfo Updates

- Existing flags used:
- New fields added:
- New flags added:
- Backward compatibility:

## 6. Middleware Changes

- Header reading:
- Bearer prefix handling:
- Missing token behavior:
- Invalid token behavior:
- Keycloak token handling:

## 7. Options and Configuration

- Section:
- Header name:
- Missing token behavior:
- Invalid token behavior:

## 8. Changed Files

- `path/to/file.cs`
  - ...

## 9. Verification

### Build
- ...

### Tests
- ...

### Manual Result
- ...

## 10. Request Example

```http
Authorization: Bearer {keycloak_api_client_token}
X-Aizen-User-Token: Bearer {identity_access_token}
```

## 11. Follow-up Notes

- ...
```
