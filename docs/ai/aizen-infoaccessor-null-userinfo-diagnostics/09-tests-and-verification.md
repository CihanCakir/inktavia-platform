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
