# 10 - Final Report

Produce the final report in this exact format:

```md
# Final Report - Aizen InfoAccessor Keycloak/User Token Separation

## 1. Root Cause

- ...

## 2. Existing Architecture Reviewed

- `Core/InfoAccessor/src/Aizen.Core.InfoAccessor`:
- `Middlewares/AizenUserInfoMiddleware`:
- Current accessor interfaces:
- Current token source:
- Current claim expectations:

## 3. New Token Model

### Keycloak API/Client Token

- Source:
- Purpose:
- Parsed by:

### Application User Token

- Source:
- Purpose:
- Parsed by:

## 4. Changed Files

- `path/to/file.cs`
  - ...

## 5. New/Updated Types

- ...

## 6. Middleware Behavior

| Scenario | Behavior |
|---|---|
| Keycloak token only | ... |
| Keycloak + valid user token | ... |
| Keycloak + invalid user token | ... |
| Missing user token | ... |

## 7. Configuration

- Section:
- Properties:
- Default user token header:

## 8. Backward Compatibility

- ...

## 9. Tests and Verification

### Build
- ...

### Tests
- ...

### Manual Checks
- ...

## 10. Migration Notes for API Consumers

- API protection token should be sent with:
  - `Authorization: Bearer {keycloak_api_client_token}`
- Application user token should be sent with:
  - `X-Aizen-User-Token: {application_user_token}`

## 11. Risks and Follow-up

- ...
```
