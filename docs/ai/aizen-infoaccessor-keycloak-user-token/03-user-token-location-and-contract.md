# 03 - User Token Location and Contract

Define how the application user token is carried and parsed.

## Requirement

The application now has two token concepts after login. The `Authorization` header is already used for Keycloak API/client token.

Therefore, the application user token must be read separately unless the existing project already has a different standard.

## Recommended User Token Header

Use a configurable header name.

Default recommendation:

```http
X-Aizen-User-Token: {application_user_token}
```

Prefer this unless the repository already uses another convention.

## Required Configuration

Add or extend options under `Core/InfoAccessor/src/Aizen.Core.InfoAccessor`.

Example:

```csharp
public sealed class AizenInfoAccessorOptions
{
    public string UserTokenHeaderName { get; set; } = "X-Aizen-User-Token";
    public string AuthorizationHeaderName { get; set; } = "Authorization";
    public bool ThrowOnMissingUserToken { get; set; } = false;
    public bool ThrowOnInvalidUserToken { get; set; } = false;
}
```

Use existing options classes if present.

## Claim Contract

Do not invent claim names. Search the Identity/Auth token generation logic and find actual claims.

## Parsing Rule

The user token reader should:

1. Read token from the configured user token header.
2. Remove `Bearer ` prefix if present.
3. Validate whether required user claims exist.
4. Populate user info only when the token is an application user token.
5. Avoid throwing on missing user token unless configured.

## Required Output

Produce:

```md
# User Token Contract Result

## User Token Header
- ...

## Options Class
- ...

## Required Claims
- ...

## Optional Claims
- ...

## Invalid/Missing Token Behavior
- ...

## Files To Change
- ...
```
