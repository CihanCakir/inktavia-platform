# 05 - Middleware Header Reading Design

Design how `AizenUserInfoMiddleware` should read the Identity token from headers.

## Required Header

Default header:

```http
X-Aizen-User-Token
```

Accepted values:

```http
X-Aizen-User-Token: {token}
X-Aizen-User-Token: Bearer {token}
```

Both should work.

## Configuration

If InfoAccessor options exist, extend them.

Recommended option:

```csharp
public string UserTokenHeaderName { get; set; } = "X-Aizen-User-Token";
```

Optional behavior options:

```csharp
public bool ThrowOnMissingUserToken { get; set; } = false;
public bool ThrowOnInvalidUserToken { get; set; } = false;
```

Use existing option style.

## Do Not Read User Token From Authorization

`Authorization` should remain reserved for Keycloak API/client token.

Only support fallback to `Authorization` if the existing project requires backward compatibility and it is explicitly documented.

## Parsing Design

The middleware should:

1. Get configured header name.
2. Read header value.
3. If missing, set empty/anonymous user info and continue.
4. Strip `Bearer ` prefix if present.
5. Validate token format using JWT handler.
6. Read claims.
7. Map claims based on the claim contract.
8. Set `AizenUserInfo`.
9. Continue pipeline.

## Safety

Do not log raw token.

Log only safe messages if logging exists.

## Required Output

Produce:

```md
# Middleware Header Reading Design

## Header Name
- ...

## Bearer Prefix Handling
- ...

## Missing Token Behavior
- ...

## Invalid Token Behavior
- ...

## Claim Mapping Strategy
- ...

## Options
- ...
```
