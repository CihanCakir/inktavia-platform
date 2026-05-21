# 06 - Implement Middleware Mapping

Now implement the middleware updates.

## Target

```text
Core/InfoAccessor/src/Aizen.Core.InfoAccessor/**/AizenUserInfoMiddleware.cs
```

## Implementation Requirements

### 1. Read User Token Header

Read from configured header, default:

```http
X-Aizen-User-Token
```

### 2. Strip Bearer Prefix

Support both:

```text
Bearer eyJ...
eyJ...
```

### 3. Parse JWT Safely

Use existing token/JWT utilities if available.

Otherwise use:

```csharp
JwtSecurityTokenHandler
```

Do not validate signature here unless the existing design already validates Identity token in middleware. If signature validation exists elsewhere, do not duplicate it blindly.

The task here is claim extraction for InfoAccessor.

### 4. Claim Mapping

Map actual claims produced by `CreateLoginToken` and `_tokenHelper`.

Expected examples may include:

```text
sub
UserId
jti
Version
RefreshTokenExpire
http://schemas.microsoft.com/ws/2008/06/identity/claims/role
```

Use case-insensitive helper methods.

### 5. Set AizenUserInfo

Populate all existing relevant fields.

Examples:

```text
UserId
Email/Username/Sub
Role
Version
RefreshTokenExpire
Jti
IsAuthenticated
IsUserTokenAvailable
IsUserTokenValid
```

Use actual properties.

### 6. Missing Token

If `X-Aizen-User-Token` is missing:

- Do not throw by default.
- Set flags appropriately if available.
- Continue request.

### 7. Invalid Token

If token cannot be parsed:

- Respect options if available.
- Otherwise do not throw by default unless current architecture expects throw.
- Set invalid flags if available.
- Continue or reject based on configuration.

### 8. Keycloak Token

Do not parse `Authorization` token as user token.

## Helper Methods

If useful, add private helpers or internal classes:

```csharp
private static string? GetClaimValue(IEnumerable<Claim> claims, params string[] claimTypes)
private static string? RemoveBearerPrefix(string value)
private static bool TryParseLong(string? value, out long result)
private static bool TryParseDateTime(string? value, out DateTime result)
```

Use existing utility classes if available.

## Required Output

Produce:

```md
# Middleware Implementation Result

## Changed Files
- ...

## Header Reading
- ...

## Claim Mapping
- ...

## AizenUserInfo Set Fields
- ...

## Missing/Invalid Token Handling
- ...

## Compatibility Notes
- ...
```
