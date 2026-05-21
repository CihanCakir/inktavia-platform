# 02 - Discovery: Login Token Generation

Do not modify code in this step.

Find and inspect:

```text
CreateLoginToken
_tokenHelper
CreateToken
CreateAccessToken
GenerateToken
GenerateAccessToken
TokenHelper
ITokenHelper
Claims
Claim
JwtSecurityToken
SecurityTokenDescriptor
```

## Main Goal

Discover exactly what values are inserted into the Identity/Application access token.

## Required Analysis

### CreateLoginToken

Find the method:

```csharp
CreateLoginToken
```

Answer:

- In which class/service is it located?
- What input does it receive?
- What `userInfo` object is used?
- Which values are passed to `_tokenHelper`?
- Does it create refresh token too?
- Does it include role/profile/agreement/device information?

### _tokenHelper

Inspect `_tokenHelper` methods used by `CreateLoginToken`.

Answer:

- Which method creates the access token?
- Which claims are added?
- Are claim names uppercase, lowercase, or mixed case?
- Are Microsoft claim type URIs used for role?
- What are issuer and audience?
- Which algorithm is used?
- Which expiration values are used?

### Claim Names

Document the actual claims written to the token.

Known example may include:

```text
sub
UserId
jti
Version
RefreshTokenExpire
http://schemas.microsoft.com/ws/2008/06/identity/claims/role
exp
iss
aud
```

But do not rely only on this example. Use the code as source of truth.

## Required Output

Produce:

```md
# Login Token Generation Report

## CreateLoginToken Location
- ...

## Token Helper Location
- ...

## Access Token Creation Method
- ...

## Values Passed From userInfo
- ...

## Claims Produced
| Claim Name | Source Value | Required? | Notes |
|---|---|---|---|

## Refresh Token Behavior
- ...

## Mapping Implications For AizenUserInfoMiddleware
- ...
```
