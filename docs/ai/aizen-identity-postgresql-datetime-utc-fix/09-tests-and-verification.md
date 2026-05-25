# 09 - Tests and Verification

Add or update tests if possible.

## Required Scenarios

### 1. Change Password Save

Run the `ChangePasswordCommandHandler` flow.

Expected:

- No Npgsql DateTime Kind error.
- Updated user/token entities save successfully.

### 2. Token Revocation

When login tokens are revoked:

Expected:

- Any date fields are UTC.
- SaveChanges succeeds.

### 3. Audit Fields

When any Identity entity is added/updated:

Expected:

- `CreateDate`, `ModifyDate`, `CreatedAt`, `UpdatedAt` or equivalent fields are UTC.

### 4. ChangeTracker Guard Test

If a test is possible, assert no tracked modified/added entity contains `DateTimeKind.Local` for persisted timestamp fields before save.

### 5. Build

Run:

```bash
dotnet build
```

### 6. Tests

Run:

```bash
dotnet test
```

## Manual Verification

Retry the API request that triggers ChangePassword.

Headers:

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

## Required Output

Produce:

```md
# Verification Report

## Build
- ...

## Tests
- ...

## ChangePassword Result
- ...

## Failing Entity Rechecked
- ...

## UTC Confirmation
- ...

## Remaining Risks
- ...
```
