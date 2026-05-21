# 08 - Tests and Verification

Add or update tests if the repository has test projects.

## Required Scenarios

### 1. Valid identity token header

Request:

```http
Authorization: Bearer {keycloak_token}
X-Aizen-User-Token: Bearer {identity_token}
```

Expected:

- `AizenUserInfo` is populated.
- `UserId` is set.
- `Role` is set if present.
- `Version` is set if present.
- `RefreshTokenExpire` is set if present.
- Flags indicate valid/authenticated user if available.

### 2. Identity token without Bearer prefix

Request:

```http
X-Aizen-User-Token: {identity_token}
```

Expected:

- Token is parsed successfully.

### 3. Missing identity token

Request:

```http
Authorization: Bearer {keycloak_token}
```

Expected:

- Middleware does not throw.
- User info is empty/anonymous.
- Flags indicate missing user token if available.

### 4. Invalid identity token

Request:

```http
X-Aizen-User-Token: invalid-token
```

Expected:

- Middleware respects configured behavior.
- No raw token logged.
- Flags indicate invalid token if available.

### 5. Claim casing

Token contains:

```text
UserId
Version
RefreshTokenExpire
```

Expected:

- Claims are read correctly.

If alternative casing exists, test:

```text
userId
version
refreshTokenExpire
```

if supported.

## Build

Run:

```bash
dotnet build
```

## Test

Run:

```bash
dotnet test
```

If tests fail due to unrelated existing issues, report clearly.

## Manual Verification

Use request:

```http
GET /api/v1/profile/me
Authorization: Bearer {keycloak_api_client_token}
X-Aizen-User-Token: Bearer {identity_access_token}
```

Expected:

- API protection passes.
- InfoAccessor resolves user information.

## Required Output

Produce:

```md
# Verification Result

## Build
- ...

## Tests
- ...

## Manual Checks
- Valid identity token:
- Missing identity token:
- Invalid identity token:
- Bearer prefix:
- Claim casing:

## Issues
- ...
```
