# 05 - Discovery: Token and Audit Date Generation

Do not modify code in this step.

Inspect token generation and audit logic.

Search for:

```text
CreateLoginToken
CreateLoginTokenAsync
_tokenHelper
CreateAccessToken
CreateRefreshToken
UserLoginTokenEntity.Create
RefreshTokenExpiredDate
AccessTokenExpiredDate
DateTime.Now
DateTime.UtcNow
DateTime.Parse
ModifyDate
CreateDate
RevokedAt
```

## Token Entity

Inspect `UserLoginTokenEntity`.

Answer:

- What date fields does it contain?
- Which fields are saved to DB?
- Which fields are nullable?
- Which fields are set on create?
- Which fields are set on revoke?
- Are expiration dates UTC?

## Token Helper

Answer:

- Does `_tokenHelper` use `DateTime.Now` or `UtcNow`?
- Does it create expiration dates with local kind?
- Does it format dates as string then parse them back?
- Does the JWT claim `RefreshTokenExpire` use local date string?
- Does that claim value get stored back into entity DateTime?

## Audit Logic

Answer:

- Where are audit fields set?
- Is `DateTime.Now` used for `CreateDate` or `ModifyDate`?
- Are base entity date properties mapped as `timestamp with time zone`?

## Required Output

Produce:

```md
# Token and Audit Date Generation Report

## Token Date Sources
- ...

## Audit Date Sources
- ...

## DateTime.Now Sources
- ...

## DateTime.UtcNow Sources
- ...

## Fields That Must Be Normalized
- ...
```
