# 09 - Tests and Verification

Add or update tests if the repository has test projects.

## Required Test Scenarios

### 1. Keycloak token only

Input:

```http
Authorization: Bearer {keycloak_client_token}
```

Expected:

- Middleware does not throw.
- Application user context is empty/anonymous.
- Optional client context is populated if implemented.

### 2. Keycloak token + valid user token

Input:

```http
Authorization: Bearer {keycloak_client_token}
X-Aizen-User-Token: {application_user_token}
```

Expected:

- Middleware does not throw.
- Application user context is populated.
- Expected claims are mapped correctly.

### 3. Keycloak token + invalid user token

Input:

```http
Authorization: Bearer {keycloak_client_token}
X-Aizen-User-Token: invalid
```

Expected depends on configuration.

### 4. Missing user token

Input:

```http
Authorization: Bearer {keycloak_client_token}
```

Expected:

- No claim missing exception.
- No failure from InfoAccessor layer.

### 5. Existing user token parsing compatibility

If old behavior parsed user token from `Authorization`, test whether backward compatibility is needed. If not implemented, document migration.

## Build and Test

Run:

```bash
dotnet build
dotnet test
```

## Required Output

Produce:

```md
# Verification Result

## Build
- ...

## Tests
- ...

## Test Scenarios
- Keycloak token only:
- Keycloak + user token:
- Invalid user token:
- Missing user token:
- Backward compatibility:

## Issues
- ...
```
