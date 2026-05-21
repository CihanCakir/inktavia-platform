# 03 - Claim Contract Map

Using the discovery from `CreateLoginToken` and `_tokenHelper`, define the exact claim-to-`AizenUserInfo` mapping.

## Required Claim Mapping

Build a map like this, using actual project names:

| AizenUserInfo Field | Primary Claim | Alternative Claims | Required |
|---|---|---|---|
| UserId | UserId | userId, nameidentifier, sub if numeric | Yes |
| Username/Email/Sub | sub | username, email, preferred_username | Optional |
| Role | role claim URI | role, roles | Optional |
| Version | Version | version | Optional |
| RefreshTokenExpire | RefreshTokenExpire | refreshTokenExpire | Optional |
| Jti | jti | JwtRegisteredClaimNames.Jti | Optional |
| Issuer | iss | issuer | Optional |
| Audience | aud | audience | Optional |

Do not assume fields that do not exist in `AizenUserInfo`.

## Case Handling

Claim reading must be case-insensitive where appropriate.

Example:

```text
UserId
userId
USERID
```

should be handled safely if the project allows.

## Role Claim Handling

Support Microsoft role claim URI if used:

```text
http://schemas.microsoft.com/ws/2008/06/identity/claims/role
```

Also support standard alternatives if existing code uses them:

```text
role
roles
ClaimTypes.Role
```

## Date Handling

If `RefreshTokenExpire` is stored as a string like:

```text
28.05.2026 10:54:41
```

parse it safely using existing culture/date conventions.

If parsing is unreliable, store raw string and only parse when existing model has a date field.

## Required Output

Produce:

```md
# Claim Contract Map

## Final Claim Mapping
| AizenUserInfo Field | Claim Source | Fallbacks | Conversion |
|---|---|---|---|

## Required Claims
- ...

## Optional Claims
- ...

## Case-Insensitive Rules
- ...

## Role Claim Rules
- ...

## Date Parsing Rules
- ...
```
